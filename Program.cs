using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using WorkoutLogger.Data;
using WorkoutLogger.Models;
using WorkoutLogger.Models.DTOs;

// Loads default configurations
var builder = WebApplication.CreateBuilder(args);

// Load connection string from User Secrets
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Checking if connection string is valid
if(string.IsNullOrWhiteSpace(connectionString)) throw new InvalidOperationException("Database connection string is not configured.");


builder.Services.AddDbContext<WorkoutsDb>(options =>
    options.UseNpgsql(connectionString,
    npgsqlOptions =>
        {
            // Enable retry on failure for transient errors
            // This means adding logic to autmatically re-attempt an operation that fails due to temporary issues (i.e:
            // Network glitches, temporary database unavailability, timeouts service throttling etc
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);

            // Set command timeout for long-running queries
            npgsqlOptions.CommandTimeout(60);
        }));


// Serialize/deserialize enums as strings (e.g. "Monday") instead of integers (e.g. 0)
// Desirializing (request body -> C#):- Accepts a string like "Monday" and converts it to the WorkoutDay.Monday enum value
// Serializing (C# -> response body): converts WorkoutDay.Monday back to "Monday" instead of 0
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Dependency Inject the Validation Enpoints Filter method
// - typeOf allows DI to resolve the type without registering each one individually
builder.Services.AddScoped(typeof(ValidationEndPointFilter<>));

builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();

var workouts = app.MapGroup("/workouts");
var workoutsId = app.MapGroup("/workouts/{id}");
var exercises = app.MapGroup("/exercises");


//Creation of Routes and methods for /WorkoutExercises
var workoutExercises = app.MapGroup("/workouts/{id:int}/exercises")
    .AddEndpointFilter(async (context, next) =>
    {
        //Validate that the id is positive
        if (context.HttpContext.Request.RouteValues["id"] is string idStr &&
            int.TryParse(idStr, out var id) && id <= 0)
        {
            return TypedResults.BadRequest("Id must be a positive integer");
        }

        return await next(context);
    });


// Creation of Routes and method for /workouts
// Gets all Workouts
workouts.MapGet("/", GetWorkouts);
// Gets a specific workout and it's exercises by id
workoutsId.MapGet("/", GetWorkoutsById);

// Updates a specific workout (pass in id of workout to update)
// Calling validationEndpointFilter to validate the model passed in (its data annotations) before the handler is called
workoutsId.MapPut("/", UpdateWorkoutById)
    .AddEndpointFilter<ValidationEndPointFilter<WorkoutDto>>();
// Creates a workout
workouts.MapPost("/", CreateWorkout)
    .AddEndpointFilter<ValidationEndPointFilter<WorkoutDto>>();
// Deletes a workout by id (pass in id of workout to delete)
workoutsId.MapDelete("/", DeleteWorkoutById);


// Get all Exercises
exercises.MapGet("/", GetExercises);

// Add a new exercise
exercises.MapPost("/", AddExcercise)
    .AddEndpointFilter<ValidationEndPointFilter<CreateExerciseDto>>();

exercises.MapPost("/bulk", AddExercises)
    .AddEndpointFilter<ValidationEndPointFilter<CreateExerciseDto>>();

// Delete an exercise by id
exercises.MapDelete("/{id}", DeleteExercise);


//Gets all exercises for a workout
app.MapGet("/workouts/exercises", GetAllWorkoutExercises);
// Creates and adds an exercise to a workout
workoutExercises.MapPost("/", AddExerciseForWokout)
    .AddEndpointFilter<ValidationEndPointFilter<WorkoutExerciseDto>>();
// Updates an exercise under a workout
workoutExercises.MapPatch("/{exerciseId}", UpdateWorkoutExercise)
    .AddEndpointFilter<ValidationEndPointFilter<UpdateWorkoutExerciseDto>>();
// Deletes an exercise from a workout
workoutExercises.MapDelete("/{exerciseId}", DeleteExerciseFromWorkout);




static async Task<IResult> GetWorkouts(WorkoutsDb db)
{
    // We specifically want just the workout.
    return TypedResults.Ok(await db.Workouts.Select(x => new WorkoutDto
    {
        Id = x.Id,
        CurrentDay = x.CurrentDay,
        DateTime = x.DateTime,
        Notes = x.Notes
    })
    .ToArrayAsync());
}

static async Task<IResult> GetWorkoutsById(int id, WorkoutsDb db)
{
    // Eager loading Workout for passed in id and its workoutexercises
    var getWorkoutWithExercises = await db.Workouts.Include(x => x.WorkoutExercises).FirstOrDefaultAsync(x => x.Id == id);

    if(getWorkoutWithExercises is null) return TypedResults.BadRequest();
    if(getWorkoutWithExercises.WorkoutExercises.Count == 0) return TypedResults.BadRequest("WorkoutExercises is null");

    // All the exercise Id's belonging to the exercises under workout
    var exerciseIds = getWorkoutWithExercises.WorkoutExercises.Select(x => x.ExerciseId).ToList();

    // Get all the exercises from the exercise db where the id is equal to any of the id's in exerciseIds
    var exercises = await db.Exercises.Where(x => exerciseIds.Contains(x.Id)).ToListAsync();

    // exercises.ForEach(ex => Console.WriteLine($"Exercise in exerciseIds: {ex.Id} name: {ex.Name}, muscle group: {ex.MuscleGroup}"));

    if(getWorkoutWithExercises is null) return TypedResults.BadRequest();

    // Mapping WorkoutExercises to WorkoutExerciseDetailDto
    var workoutExerciseDetails = getWorkoutWithExercises.WorkoutExercises.Select(x => new WorkoutExerciseDetailDto
    {
        Name = exercises.Where(y => y.Id == x.ExerciseId).Select(y => y.Name).Single(),
        MuscleGroup = exercises.Where(y => y.Id == x.ExerciseId).Select(x => x.MuscleGroup).Single(),
        Sets = x.Sets,
        Reps = x.Reps,
        WeightKg = x.WeightKg
    }).ToList();


    if(workoutExerciseDetails.Count == 0) return TypedResults.BadRequest("No exercises found for workout");

    // Making a WorkoutWithExerciseDto
    var workoutWithExercises = new WorkoutWithExerciseDto
    {
        Day = getWorkoutWithExercises.CurrentDay,
        DateTime = getWorkoutWithExercises.DateTime,
        Notes = getWorkoutWithExercises.Notes,
        Exercises = workoutExerciseDetails ?? new List<WorkoutExerciseDetailDto>() {}
    };

    return TypedResults.Ok(workoutWithExercises);
}

static async Task<IResult> UpdateWorkoutById(int id, CreateWorkoutDto workoutDto, WorkoutsDb db)
{
    // Check if workoutDto is null
    // if(workoutDto is null) return TypedResults.BadRequest("workout is null");

    // Check if passed in Id exists in workouts database
    var workout = await db.Workouts.FindAsync(id);

    if(workout != null)
    {
        workout.CurrentDay = workoutDto.CurrentDay!.Value;
        workout.DateTime = workoutDto.DateTime!.Value;
        workout.Notes = workoutDto.Notes ?? string.Empty;
        await db.SaveChangesAsync();
        return TypedResults.NoContent();

    } else {
        return TypedResults.BadRequest("WorkoutId does not exist");
    }
}


static async Task<IResult> CreateWorkout(CreateWorkoutDto workoutDTO, WorkoutsDb db)
{
    // if(workoutDTO is null) return TypedResults.BadRequest();

    var workout = new Workout
    {
        CurrentDay = workoutDTO.CurrentDay!.Value,
        DateTime = workoutDTO.DateTime!.Value,
        Notes = workoutDTO.Notes ?? string.Empty,
    };

    db.Workouts.Add(workout);
    await db.SaveChangesAsync();

    return TypedResults.Created($"workouts/{workout.Id}", workout);
}

static async Task<IResult> DeleteWorkoutById(int id, WorkoutsDb db)
{
    // Find the workout by id - then remove it from workout db set then save the changes
    var workoutToRemove = await db.Workouts.FindAsync(id);
    if(workoutToRemove != null)
    {
        db.Workouts.Remove(workoutToRemove);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
    return TypedResults.NotFound();
}

static async Task<IResult> GetExercises(WorkoutsDb db)
{
    List<ExerciseDto> getExercises = new List<ExerciseDto>();
    var exercises = await db.Exercises.ToArrayAsync();

    foreach(var exercise in exercises)
    {
        var newExercise = new ExerciseDto
        {
            Id = exercise.Id,
            Name = exercise.Name,
            MuscleGroup = exercise.MuscleGroup,
        };
        getExercises.Add(newExercise);
    }

    return TypedResults.Ok(getExercises);
}

static async Task<IResult> AddExcercise(CreateExerciseDto exerciseDTO, WorkoutsDb db)
{
    // //Check if excercise passed in is null
    // if(exerciseDTO is null) return TypedResults.BadRequest();

    // //Check if fields is empty, null or whitespace
    // if(string.IsNullOrWhiteSpace(exerciseDTO.Name) || string.IsNullOrWhiteSpace(exerciseDTO.MuscleGroup))
    // {
    //     return TypedResults.BadRequest();
    // }

    // Check if the exercise already exists within the workout - prevent duplicate exercises
    if(await db.Exercises.AnyAsync(x => string.Equals(x.Name, exerciseDTO.Name, StringComparison.OrdinalIgnoreCase))) return TypedResults.BadRequest("Exercise already exists");

    // Create new excercise model
    var exercise = new Exercise
    {
        Name = exerciseDTO.Name!,
        MuscleGroup = exerciseDTO.MuscleGroup!,
    };

    // Add new exercise model into the database and save changes
    db.Exercises.Add(exercise);
    await db.SaveChangesAsync();

    // Return 201 status code and message stating excercise id created
    return TypedResults.Created($"exercises/{exercise.Id}", exercise.Id);
}

static async Task<IResult> AddExercises(List<CreateExerciseDto> exerciseDtos, WorkoutsDb db)
{
    // Adding a list of exercises to our db at once.
    if(exerciseDtos.Count == 0) return TypedResults.BadRequest("Empty Json data");

    var workouts = exerciseDtos.Select(dto => new Exercise
    {
        Name = dto.Name,
        MuscleGroup = dto.MuscleGroup
    }).ToList();

    db.Exercises.AddRange(workouts);
    await db.SaveChangesAsync();

    return Results.Ok(workouts);
};

static async Task<IResult> DeleteExercise(int id, WorkoutsDb db)
{
    // Find the excercise and store it in a variable
    var excercise = await db.Exercises.FindAsync(id);

    // Check if that variable is not null
    if(excercise != null)
    {
        //Remove excercise from dbSet
        db.Exercises.Remove(excercise);
        // Save changes to database
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}



static async Task<IResult> GetAllWorkoutExercises(WorkoutsDb db)
{
    return TypedResults.Ok(await db.WorkoutExercises.ToListAsync());
}

static async Task<IResult> AddExerciseForWokout(int Id, WorkoutExerciseDto workoutExerciseDTO, WorkoutsDb db)
{
    if(workoutExerciseDTO is null) return TypedResults.BadRequest();

    // Checking if workoutId & exerciseId exists already as a combination within WorkoutExercise table
    var doesWorkoutExercisseExists = await db.WorkoutExercises.Where(x => x.WorkoutId == Id && x.ExerciseId == workoutExerciseDTO.ExerciseId).AnyAsync();

    if(doesWorkoutExercisseExists) return TypedResults.BadRequest();

    //Check if Id passed in for WorkoutId exists in Workouts database
    var workoutIdIsValid = await db.Workouts.FindAsync(Id);
    // Checking if Exercise ID in dto exists in Exercise database
    var exerciseIdIsValid = await db.Exercises.FindAsync(workoutExerciseDTO.ExerciseId);

    // If workoutId or exerciseId does not exist in their retrospective databases, return
    if(workoutIdIsValid is null || exerciseIdIsValid is null) return TypedResults.NotFound();

    var workoutExercise = new WorkoutExercise
    {
        WorkoutId = Id,
        ExerciseId = workoutExerciseDTO.ExerciseId,
        Sets = workoutExerciseDTO.Sets,
        Reps = workoutExerciseDTO.Reps,
        WeightKg = workoutExerciseDTO.WeightKg
    };

    db.WorkoutExercises.Add(workoutExercise);
    await db.SaveChangesAsync();

    return TypedResults.Created($"/exercise/{workoutExercise.Id}", workoutExercise);
}

static async Task<IResult> UpdateWorkoutExercise(int Id, int exerciseId, UpdateWorkoutExerciseDto updateWorkoutExerciseDTO, WorkoutsDb db)
{
    if(updateWorkoutExerciseDTO is null) return TypedResults.BadRequest();

    var workoutExerciseToUpdate = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.WorkoutId == Id && x.ExerciseId == exerciseId);

    if(workoutExerciseToUpdate is null) return TypedResults.NotFound();

    workoutExerciseToUpdate.Sets = updateWorkoutExerciseDTO.Sets;
    workoutExerciseToUpdate.Reps = updateWorkoutExerciseDTO.Reps;
    workoutExerciseToUpdate.WeightKg = updateWorkoutExerciseDTO.WeightKg;

    await db.SaveChangesAsync();
    return TypedResults.NoContent();
}

static async Task<IResult> DeleteExerciseFromWorkout(int Id, int exerciseId, WorkoutsDb db)
{
    var exerciseToDelete = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.WorkoutId == Id && x.ExerciseId == exerciseId);
    if(exerciseToDelete is null) return TypedResults.NotFound();

    db.WorkoutExercises.Remove(exerciseToDelete);
    await db.SaveChangesAsync();

    return TypedResults.NoContent();
}

app.Run();
