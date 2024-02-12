using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.Tests
{
    [TestFixture]
    public class NUnitIntegrationTests
    {
        private MoviesLibraryNUnitTestDbContext _dbContext;
        private IMoviesLibraryController _controller;
        private IMoviesRepository _repository;
        IConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        [SetUp]
        public async Task Setup()
        {
            string dbName = $"MoviesLibraryTestDb_{Guid.NewGuid()}";
            _dbContext = new MoviesLibraryNUnitTestDbContext(_configuration, dbName);

            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Test]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == movie.Title).FirstOrDefaultAsync();
            Assert.IsNotNull(resultMovie);
        }

        [Test]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                // No title
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5

                // Provide an invalid movie object, for example, missing required fields like 'Title'
                // Assuming 'Title' is a required field, do not set it
            };

            // Act and Assert
            // Expect a ValidationException because the movie is missing a required field
            var exception = Assert.ThrowsAsync<ValidationException>(() => _controller.AddAsync(invalidMovie));
        }

        [Test]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange            
            var movie = new Movie
            {
                Title = "Naruto",
                Director = "Denis Atanassov",
                YearReleased = 1992,
                Genre = "Anime",
                Duration = 120,
                Rating = 5.5
            };
            await _controller.AddAsync(movie);
            // Act            
            await _controller.DeleteAsync(movie.Title);
            // Assert
            // The movie should no longer exist in the database
            var result = await _dbContext.Movies.Find(m => m.Title == movie.Title).FirstOrDefaultAsync();
            Assert.Null(result);
        }


        [Test]
        public async Task DeleteAsync_WhenTitleIsNull_ShouldThrowArgumentException()
        {
            // Act and Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(null));
            Assert.AreEqual("Title cannot be empty.", exception.Message);
        }

        [Test]
        public async Task DeleteAsync_WhenTitleIsEmpty_ShouldThrowArgumentException()
        {
            // Act and Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(""));
            Assert.AreEqual("Title cannot be empty.", exception.Message);
        }

        [Test]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            // Act and Assert
            const string nonExistingTitle = "Non Existing title";
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => _controller.DeleteAsync(nonExistingTitle));
            Assert.AreEqual($"Movie with title '{nonExistingTitle}' not found.", exception.Message);
        }

        [Test]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetAllAsync_WhenMoviesExist_ShouldReturnAllMovies()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Naruto",
                Director = "Denis Atanassov",
                YearReleased = 1992,
                Genre = "Anime",
                Duration = 119,
                Rating = 6.5
            };
            await _controller.AddAsync(movie1);

            var movie2 = new Movie
            {
                Title = "Sasuke",
                Director = "Denis Atanassov",
                YearReleased = 1994,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            await _controller.AddAsync(movie2);

            // Act
            var allMovies = await _controller.GetAllAsync();
            // Assert
            // Ensure that all movies are returned
            Assert.IsNotEmpty(allMovies);
            Assert.AreEqual(2, allMovies.Count());

            var hasFirstMovie = allMovies.Any(x => x.Title == movie1.Title 
                                                && x.Director == movie1.Director
                                                && x.YearReleased == movie1.YearReleased
                                                && x.Genre == movie1.Genre
                                                && x.Duration == movie1.Duration
                                                && x.Rating == movie1.Rating);
            Assert.IsTrue(hasFirstMovie);

            var hasSecondMovie = allMovies.Any(y => y.Title == movie2.Title
                                                && y.Director == movie2.Director
                                                && y.YearReleased == movie2.YearReleased
                                                && y.Genre == movie2.Genre
                                                && y.Duration == movie2.Duration
                                                && y.Rating == movie2.Rating);
            Assert.IsTrue(hasSecondMovie);
            
        }

        [Test]
        public async Task GetByTitle_WhenTitleExists_ShouldReturnMatchingMovie()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Naruto",
                Director = "Denis Atanassov",
                YearReleased = 1992,
                Genre = "Anime",
                Duration = 119,
                Rating = 6.5
            };
            await _controller.AddAsync(movie1);
            // Act
            var result = await _controller.GetByTitle(movie1.Title);
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(movie1.Title, result.Title);
            Assert.AreEqual(movie1.Director, result.Director);
            Assert.AreEqual(movie1.YearReleased, result.YearReleased);
            Assert.AreEqual(movie1.Genre, result.Genre);
            Assert.AreEqual(movie1.Duration, result.Duration);
            Assert.AreEqual(movie1.Rating, result.Rating);
        }

        [Test]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act
            var resultMovie = await _controller.GetByTitle("Fake Title");
            // Assert

            Assert.IsNull(resultMovie);
        }


        [Test]
        public async Task SearchByTitleFragmentAsync_WhenTitleFragmentExists_ShouldReturnMatchingMovies()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Naruto",
                Director = "Denis Atanassov",
                YearReleased = 1992,
                Genre = "Anime",
                Duration = 119,
                Rating = 6.5
            };
            //await _controller.AddAsync(movie1);

            var movie2 = new Movie
            {
                Title = "Sasuke",
                Director = "Denis Atanassov",
                YearReleased = 1994,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            //await _controller.AddAsync(movie2);
            await _dbContext.Movies.InsertManyAsync(new[] { movie1, movie2 }); // directno insert prez _dbCOntext ne prez controllera v DB

            // Act
            var result = await _controller.SearchByTitleFragmentAsync("Nar"); // fragment Nar ot Naruto
            // Assert // Should return one matching movie
            Assert.IsNotEmpty(result);
            Assert.AreEqual(1, result.Count());
            
            var resultMovie = result.First();
            Assert.AreEqual(movie1.Title, resultMovie.Title);
            Assert.AreEqual(movie1.Director, resultMovie.Director);
            Assert.AreEqual(movie1.YearReleased, resultMovie.YearReleased);
            Assert.AreEqual(movie1.Genre, resultMovie.Genre);
            Assert.AreEqual(movie1.Duration, resultMovie.Duration);
            Assert.AreEqual(movie1.Rating, resultMovie.Rating);
        }

        [Test]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            var exception = Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.SearchByTitleFragmentAsync("Does not exist"));
            Assert.AreEqual("No movies found.", exception.Message);
        }

        [Test]
        public async Task UpdateAsync_WhenValidMovieProvided_ShouldUpdateMovie()
        {
            // Arrange
            var movie1 = new Movie
            {
                Title = "Naruto",
                Director = "Denis Atanassov",
                YearReleased = 1992,
                Genre = "Anime",
                Duration = 119,
                Rating = 6.5
            };
            //await _controller.AddAsync(movie1);

            var movie2 = new Movie
            {
                Title = "Sasuke",
                Director = "Denis Atanassov",
                YearReleased = 1994,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            await _dbContext.Movies.InsertManyAsync(new[] { movie1, movie2 });

            // Modify the movie
            var movieToUpdate = await _dbContext.Movies.Find(x => x.Title == movie1.Title).FirstOrDefaultAsync();

            movie1.Title = "Naruto Shipuuden";
            movie1.Rating = 10;

            // Act
            await _controller.UpdateAsync(movieToUpdate);

            // Assert
            var result = await _dbContext.Movies.Find(x => x.Title == movieToUpdate.Title).FirstOrDefaultAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(movieToUpdate.Title, result.Title);
            Assert.AreEqual(movieToUpdate.Rating, result.Rating);
        }

        [Test]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange // Movie without required fields
            var invalidMovie = new Movie
            {
                
                Director = "Denis Atanassov",
                YearReleased = 1992,
                Genre = "Anime",
                Duration = 119,
                Rating = 6.5
            };


            // Act and Assert
            var exception = Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAsync(invalidMovie));
            Assert.AreEqual("Movie is not valid.", exception.Message);

        }


        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }
    }
}
