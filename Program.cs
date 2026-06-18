using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using WorkoutLogger.Data;
using WorkoutLogger.Models;
using WorkoutLogger.Models.DTOs;
using WorkoutLogger.EndpointFilter;
using Microsoft.IdentityModel.Tokens;
using WorkoutLogger.Models.Auth;
using WorkoutLogger.Security;
using Npgsql.Replication;

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

// Configure JSON options globally
builder.Services.ConfigureHttpJsonOptions(options =>
{
    // Serialize/deserialize enums as strings (e.g. "Monday") instead of integers (e.g. 0)
    // Desirializing (request body -> C#):- Accepts a string like "Monday" and converts it to the WorkoutDay.Monday enum value
    // Serializing (C# -> response body): converts WorkoutDay.Monday back to "Monday" instead of 0
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.WriteIndented = true; // Optional for readability
});

builder.Services.AddSwaggerGen(options =>
{
    // Path to the generated XML file
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
    // Display enums as strings in Swagger UI
    options.UseInlineDefinitionsForEnums();
});

// Dependency Inject the Validation Enpoints Filter method
// - typeOf allows DI to resolve the type without registering each one individually
builder.Services.AddScoped(typeof(ValidationEndPointFilter<>));

// This tool .AddEndpointsApiExplorer registers services that describe endpoints directly mapped via app.MapGet, app.MapPost
builder.Services.AddEndpointsApiExplorer(); // Required for Swagger to discover endpoints


var app = builder.Build();

if(app.Environment.IsDevelopment())
{
    // Enable Swagger UI (only in development for production, wrap in app.Environment.IsDevelopment())
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Workout Logger Api");
        options.RoutePrefix = ""; // Serve Swagger UI at the root (e.g., https://localhost:5001)
    });
}

var workouts = app.MapGroup("/workouts");
var workoutsId = app.MapGroup("/workouts/{id}");
var exercises = app.MapGroup("/exercises");
var auth = app.MapGroup("/auth");


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
workouts.MapGet("/", GetWorkouts)
    .WithSummary("Returns all Workouts")
    .WithDescription("This endpoint returns all workouts in the Workouts table")
    .Produces<WorkoutDto[]>(StatusCodes.Status200OK);

// Creates a workout
workouts.MapPost("/", CreateWorkout)
    .AddEndpointFilter<ValidationEndPointFilter<WorkoutDto>>()
    .WithSummary("Creates a new Workout")
    .WithDescription("This endpoint creates a new Workout in the Workouts table")
    .Produces<WorkoutDto>(StatusCodes.Status201Created); // Response type


// Gets a specific workout and it's exercises by id
workoutsId.MapGet("/", GetWorkoutsById)
    .WithSummary("Returns Workout with matching Id")
    .WithDescription("This endpoint returns the Workout for the Id passed into the url")
    .Produces<WorkoutWithExerciseDto>(StatusCodes.Status200OK) // Response type
    .Produces(StatusCodes.Status404NotFound) // Possible error
    .Produces(StatusCodes.Status400BadRequest); // Possible error

// Updates a specific workout (pass in id of workout to update)
// Calling validationEndpointFilter to validate the model passed in (its data annotations) before the handler is called
workoutsId.MapPut("/", UpdateWorkoutById)
    .AddEndpointFilter<ValidationEndPointFilter<WorkoutDto>>()
    .WithSummary("Updates Workout with matching Id")
    .WithDescription("This endpoint updates the Workout for the Id passed into the url")
    .Produces(StatusCodes.Status204NoContent) // Response type
    .Produces(StatusCodes.Status404NotFound); // Possible error

// Deletes a workout by id (pass in id of workout to delete)
workoutsId.MapDelete("/", DeleteWorkoutById)
    .WithSummary("Deletes a Workout with the matching Id")
    .WithDescription("This endpoint deletes the Workout for the Id passed into the url")
    .Produces(StatusCodes.Status204NoContent) // Response type
    .Produces(StatusCodes.Status404NotFound); // Possible error


// Get all Exercises
exercises.MapGet("/", GetExercises)
    .WithSummary("Returns all Exercises")
    .WithDescription("This endpoint returns all Exercises in the Exercises table")
    .Produces<List<ExerciseDto>>(StatusCodes.Status200OK);

// Add a new exercise
exercises.MapPost("/", AddExcercise)
    .AddEndpointFilter<ValidationEndPointFilter<CreateExerciseDto>>()
    .WithSummary("Create a new Exercise")
    .WithDescription("This endpoint creates a new Exercise in the Exercises table")
    .Produces<int>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest);

exercises.MapPost("/bulk", AddExercises)
    .AddEndpointFilter<ValidationEndPointFilter<List<CreateExerciseDto>>>()
    .WithSummary("Create a list of new Exercises")
    .WithDescription("This endpoint creates a list of new Exercises in the Exercises table")
    .Produces<List<Exercise>>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest);

// Delete an exercise by id
exercises.MapDelete("/{id}", DeleteExercise)
    .WithSummary("Deletes an Exercise with the matching Id")
    .WithDescription("This endpoint deletes the Exercise for the Id passed into the url")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);


//Gets all exercises for a workout
app.MapGet("/workouts/exercises", GetAllWorkoutExercises)
    .WithSummary("Returns all WorkoutExercises")
    .WithDescription("This endpoint returns all Workout Exercises in the WorkoutExercises table")
    .Produces<List<WorkoutExercisesDto>>(StatusCodes.Status200OK);


// Creates and adds an exercise to a workout
workoutExercises.MapPost("/", AddExerciseForWokout)
    .AddEndpointFilter<ValidationEndPointFilter<CreateWorkoutExerciseDto>>()
    .WithSummary("Create a new WorkoutExercise")
    .WithDescription("This endpoint creates a new WorkoutExercise in the WorkoutExercises table")
    .Produces<WorkoutExercise>(StatusCodes.Status201Created)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound);


// Updates an exercise under a workout
workoutExercises.MapPatch("/{exerciseId}", UpdateWorkoutExercise)
    .AddEndpointFilter<ValidationEndPointFilter<UpdateWorkoutExerciseDto>>()
    .WithSummary("Updates WorkoutExercise with matching Id")
    .WithDescription("This endpoint updates the WorkoutExercise for the Id passed into the url")
    .Produces(StatusCodes.Status204NoContent) // Response type
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status404NotFound); // Possible error

// Deletes an exercise from a workout
workoutExercises.MapDelete("/{exerciseId}", DeleteExerciseFromWorkout)
    .WithSummary("Deletes a WorkoutExercise with the matching Id")
    .WithDescription("This endpoint deletes the Exercise for the Id passed into the url")
    .Produces(StatusCodes.Status204NoContent)
    .Produces(StatusCodes.Status404NotFound);


// Create an authentication register endpoint where user sends an CreateUserDto to the endpoint and it is added to the database and the password is encrpted/ hashed
auth.MapPost("/register", RegisterUser)
    .AddEndpointFilter<ValidationEndPointFilter<RegisterUserDto>>();

auth.MapPost("/login", LoginUser)
    .AddEndpointFilter<ValidationEndPointFilter<LoginUserDto>>();

//Create an authentication login endpoint where the user logs in and is given a jwt token  if credentials are valid/ exist in database




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
    var getWorkoutWithExercises = await db.Workouts.Include(x => x.WorkoutExercises).ThenInclude(x => x.Exercise).FirstOrDefaultAsync(x => x.Id == id);

    if(getWorkoutWithExercises is null) return TypedResults.NotFound("Workout Id does not exists in Workouts table");

    // Mapping WorkoutExercises to WorkoutExerciseDetailDto
    var workoutExerciseDetails = getWorkoutWithExercises.WorkoutExercises.Select(x => new WorkoutExerciseDetails
    {
        Name = x.Exercise.Name,
        MuscleGroup = x.Exercise.MuscleGroup,
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
        Exercises = workoutExerciseDetails ?? new List<WorkoutExerciseDetails>() {}
    };

    return TypedResults.Ok(workoutWithExercises);
}

static async Task<IResult> UpdateWorkoutById(int id, CreateWorkoutDto workoutDto, WorkoutsDb db)
{

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
        return TypedResults.NotFound("WorkoutId does not exist");
    }
}


static async Task<IResult> CreateWorkout(CreateWorkoutDto workoutDTO, WorkoutsDb db)
{

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
        Name = dto.Name!,
        MuscleGroup = dto.MuscleGroup!
    }).ToList();

    db.Exercises.AddRange(workouts);
    await db.SaveChangesAsync();

    return Results.Ok(workouts);
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



static async Task<IResult> GetAllWorkoutExercises(WorkoutsDb db)
{
    return TypedResults.Ok(await db.WorkoutExercises.Select(x => new WorkoutExercisesDto
    {
        Id = x.Id,
        WorkoutId = x.WorkoutId,
        ExerciseId = x.ExerciseId,
        Sets = x.Sets,
        Reps = x.Reps,
        WeightKg = x.WeightKg,
    })
    .ToListAsync());
}

static async Task<IResult> AddExerciseForWokout(int Id, CreateWorkoutExerciseDto workoutExerciseDTO, WorkoutsDb db)
{
    if(workoutExerciseDTO is null) return TypedResults.BadRequest("Json data is null");

    // Checking if workoutId & exerciseId exists already as a combination within WorkoutExercise table
    var doesWorkoutExerciseExists = await db.WorkoutExercises.Where(x => x.WorkoutId == Id && x.ExerciseId == workoutExerciseDTO.ExerciseId).AnyAsync();

    if(doesWorkoutExerciseExists) return TypedResults.BadRequest("The exercise and workout already exists");

    //Check if Id passed in for WorkoutId exists in Workouts database
    var workoutIdIsValid = await db.Workouts.FindAsync(Id);
    // Checking if Exercise ID in dto exists in Exercise database
    var exerciseIdIsValid = await db.Exercises.FindAsync(workoutExerciseDTO.ExerciseId);

    // If workoutId or exerciseId does not exist in their retrospective databases, return
    if(workoutIdIsValid is null || exerciseIdIsValid is null) return TypedResults.NotFound("Workout Id or Exercise id does not exist in their retrospective databases");

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

    workoutExerciseToUpdate.Sets = updateWorkoutExerciseDTO.Sets is null ? workoutExerciseToUpdate.Sets : updateWorkoutExerciseDTO.Sets!.Value;
    workoutExerciseToUpdate.Reps = updateWorkoutExerciseDTO.Reps is null ? workoutExerciseToUpdate.Reps : updateWorkoutExerciseDTO.Reps!.Value;
    workoutExerciseToUpdate.WeightKg = updateWorkoutExerciseDTO.WeightKg is null ? workoutExerciseToUpdate.WeightKg : updateWorkoutExerciseDTO.WeightKg!.Value;

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



static async Task<IResult> RegisterUser(RegisterUserDto registerUserDto, WorkoutsDb db)
{
    if(string.IsNullOrWhiteSpace(registerUserDto.Username) || string.IsNullOrWhiteSpace(registerUserDto.Password))
        return TypedResults.BadRequest("Username and Password are required");

    var normalizedUsername = registerUserDto.Username!.Trim().ToLower();
    var password = registerUserDto.Password!;

    // Check if username already exists in the database if it does return username already exists
    var existing = await db.Users.Where(x => x.UserName == normalizedUsername).FirstOrDefaultAsync();

    if(existing != null) return TypedResults.BadRequest("User with this username already exists");

    var userEntity = new User
    {
        UserName = normalizedUsername
    };

    userEntity.PasswordHash = PasswordHashing.HashPassword(userEntity, password);

    db.Users.Add(userEntity);
    await db.SaveChangesAsync();

    return TypedResults.Created("/auth/register", registerUserDto);
}


static async Task<IResult> LoginUser(LoginUserDto loginUserDto, WorkoutsDb db)
{
    if(string.IsNullOrWhiteSpace(loginUserDto.Username) || string.IsNullOrWhiteSpace(loginUserDto.Password))
    {
        throw new ArgumentNullException(
            $"{nameof(loginUserDto.Username)} or {nameof(loginUserDto.Password)} cannot be empty",
            innerException: null);
    }

    var registeredUser = await db.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == loginUserDto.Username.ToLower());

    if(registeredUser is null) return TypedResults.BadRequest("Username does not exist");

    var registeredUserPassword = registeredUser.PasswordHash;

    var result = PasswordHashing.VerifyPassword(registeredUserPassword,
                                                loginUserDto.Password);

    Console.WriteLine("This is the result" + result);
    // Implement loggin user and then implememnt adding jwt keys
    //Check if the username exists - paths for yes or no
    // if username exists check if the password sent for the username matches the password stored in database for that user

    // If password matches give user a jwt token if it doesnt then user details are incorrect tell them to log in again


    return TypedResults.Ok("The result is " + result);
}

app.Run();
