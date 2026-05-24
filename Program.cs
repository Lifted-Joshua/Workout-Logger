using Microsoft.EntityFrameworkCore;
using WorkoutLogger.Data;
using WorkoutLogger.Models;
using WorkoutLogger.Models.DTOs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<WorkoutsDb>(options => options.UseInMemoryDatabase("Workout Database"));



var app = builder.Build();

var workouts = app.MapGroup("/workouts");

// Creation of Routes and method for /workouts
// Gets all Workouts
workouts.MapGet("/", GetWorkouts);

// Gets a specific workout and it's exercises by id
workouts.MapGet("/{id}", GetWorkoutsById);
// Updates a specific workout
workouts.MapPut("/{id}", UpdateWorkoutById);
// Creates a workout
workouts.MapPost("/", CreateWorkout);
// Deletes a workout by id
workouts.MapDelete("/{id}", DeleteWorkoutById);


//Creation of Routes and methods for /exercises
var exercises = app.MapGroup("/exercises");
// Get all Exercises
exercises.MapGet("/", GetExercises);

// Add a new exercise
exercises.MapPost("/", AddExcercise);

// Delete an exercise by id
exercises.MapDelete("/{id}", DeleteExercise);


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


//Gets all exercises for a workout
workoutExercises.MapGet("/", GetAllExercisesForWorkout);

// Creates and adds an exercise to a workout
workoutExercises.MapPost("/", AddExerciseForWokout);

// Updates an exercise under a workout
workoutExercises.MapPatch("/{exerciseId}", UpdateWorkoutExercise);

// Deletes an exercise from a workout
workoutExercises.MapDelete("/{exerciseId}", DeleteExerciseFromWorkout);


static async Task<IResult> GetWorkouts(WorkoutsDb db)
{
    return TypedResults.Ok(await db.Workouts.ToArrayAsync());
}

static async Task<IResult> GetWorkoutsById(int id, WorkoutsDb db)
{
    List<ExerciseDto> exercises = new List<ExerciseDto>();

    // Check if the passed in id(workoutId) exists in the Workouts db
    var workout = await db.Workouts.FindAsync(id);

    if(workout is null) return TypedResults.NotFound();

    // If the workout exists in the workouts database get the exercises for it
    // Find the item in the database with the Id - if it is not null do mapping, else return NotFound()
    // Step 1: Get all ExerciseIds that belong to the given WorkoutId and return it as a list
    var exerciseIds = await db.WorkoutExercises.Where(x => x.WorkoutId == id).Select(x => x.ExerciseId).ToListAsync();

    // Step 2: Use the list of ExerciseIds to get the matching Exercise records
    // Filters WorkoutExercises table/dbSet where the workoutExercise's ExerciseId exists in the list of exerciseId's
    // (the exeriseId's list is a list storing all the ExerciseId's that belong to a specific workout)
    var getWorkoutExercises = await db.WorkoutExercises.Where(x => exerciseIds.Contains(x.ExerciseId) && x.WorkoutId == id).ToListAsync();

    // Get the exercises from Exercise table where the Id matches the exercisesId's list that stores the Id's belonging to the workout passed in
    var getExercises = await db.Exercises.Where(x => exerciseIds.Contains(x.Id)).ToListAsync();


    foreach(var exercise in getWorkoutExercises)
    {
        var name = getExercises.Where(x => x.Id == exercise.ExerciseId).Select(x => x.Name).Single();
        var muscleGroup = getExercises.Where(x => x.Id == exercise.ExerciseId).Select(x => x.MuscleGroup).Single();

        exercises.Add(new ExerciseDto
        {
            Name = name,
            MuscleGroup = muscleGroup,
            Sets = exercise.Sets,
            Reps = exercise.Reps,
            WeightKg = exercise.WeightKg,
        });
    }

    var workoutWithExercises = new WorkoutWithExerciseDto
    {
        Day = workout.CurrentDay,
        DateTime = workout.DateTime,
        Notes = workout.Notes,
        Exercises = exercises
    };

    return TypedResults.Ok(workoutWithExercises);

}

static async Task<IResult> UpdateWorkoutById(int id, WorkoutDto workoutDto, WorkoutsDb db)
{
    // Check if workoutDto is null
    if(workoutDto is null) return TypedResults.BadRequest("workout is null");

    // Check if passed in Id exists in workouts database
    var workout = await db.Workouts.FindAsync(id);

    if(workout != null)
    {
        workout.CurrentDay = workoutDto.CurrentDay;
        workout.DateTime = workoutDto.DateTime;
        workout.Notes = workoutDto.Notes ?? string.Empty;
        await db.SaveChangesAsync();
        return TypedResults.NoContent();

    } else {
        return TypedResults.BadRequest("WorkoutId does not exist");
    }


}

static async Task<IResult> CreateWorkout(WorkoutDto workoutDTO, WorkoutsDb db)
{
    if(workoutDTO is null) return TypedResults.BadRequest();

    var workout = new Workout
    {
        CurrentDay = workoutDTO.CurrentDay,
        DateTime = workoutDTO.DateTime,
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
    var exercises = await db.Exercises.ToArrayAsync();

    return TypedResults.Ok(exercises);
}

static async Task<IResult> AddExcercise(ExerciseDto exerciseDTO, WorkoutsDb db)
{
    //Check if excercise passed in is null
    if(exerciseDTO is null) return TypedResults.BadRequest();

    //Check if fields is empty, null or whitespace
    if(string.IsNullOrWhiteSpace(exerciseDTO.Name) && string.IsNullOrWhiteSpace(exerciseDTO.MuscleGroup))
    {
        return TypedResults.BadRequest();
    }

    // Check if the exercise already exists within the workout - prevent duplicate exercises
    if(await db.Exercises.AnyAsync(x => string.Equals(x.Name, exerciseDTO.Name, StringComparison.OrdinalIgnoreCase))) return TypedResults.BadRequest();

    // Create new excercise model
    var exercise = new Exercise
    {
        Name = exerciseDTO.Name,
        MuscleGroup = exerciseDTO.MuscleGroup,
    };

    // Add new exercise model into the database and save changes
    db.Exercises.Add(exercise);
    await db.SaveChangesAsync();

    // Return 201 status code and message stating excercise id created
    return TypedResults.Created($"excercises/{exercise.Id}", exercise.Id);
}

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



static async Task<IResult> GetAllExercisesForWorkout(int id, WorkoutsDb db)
{
    // Step 1: Get all ExerciseIds that belong to the given WorkoutId
    // This queries the WorkoutExercises table and extracts only the ExerciseId values
    var exerciseId = await db.WorkoutExercises.Where(x => x.WorkoutId == id).Select(x => x.ExerciseId).ToListAsync();

    // Step 2: Use the list of ExerciseIds to get the matching Exercise records
    // Filters Exercises where the Id exists in the list collected above
    var getExercises = await db.Exercises.Where(x => exerciseId.Contains(x.Id)).ToListAsync();

    if(getExercises.Count == 0) return TypedResults.Ok(getExercises);

    return TypedResults.NotFound();
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
