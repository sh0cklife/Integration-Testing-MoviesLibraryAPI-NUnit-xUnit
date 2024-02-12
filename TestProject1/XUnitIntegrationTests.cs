using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.XUnitTests
{
    public class XUnitIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly MoviesLibraryXUnitTestDbContext _dbContext;
        private readonly IMoviesLibraryController _controller;
        private readonly IMoviesRepository _repository;

        public XUnitIntegrationTests(DatabaseFixture fixture)
        {
            _dbContext = fixture.DbContext;
            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);

            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabaseAsync()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Fact]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            Xunit.Assert.NotNull(resultMovie);
            Xunit.Assert.Equal(movie.Title, resultMovie.Title);
            Xunit.Assert.Equal(movie.Director, resultMovie.Director);
            Xunit.Assert.Equal(movie.YearReleased, resultMovie.YearReleased);
            Xunit.Assert.Equal(movie.Genre, resultMovie.Genre);
            Xunit.Assert.Equal(movie.Duration, resultMovie.Duration);
            Xunit.Assert.Equal(movie.Rating, resultMovie.Rating);
        }

        [Fact]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                // Provide an invalid movie object, e.g., without a title or other required fields

                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };


            // Act and Assert
            var exception = await Xunit.Assert.ThrowsAsync<ValidationException>(() => _controller.AddAsync(invalidMovie));
            Xunit.Assert.Equal("Movie is not valid.", exception.Message);
        }

        [Fact]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange            
            var movie = new Movie
            {
                Title = "Naruto Shipuuden",
                Director = "Denis Atanassov",
                YearReleased = 2022,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            // Act            
            await _controller.AddAsync(movie);
            await _controller.DeleteAsync(movie.Title);

            // Assert
            // The movie should no longer exist in the database
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == movie.Title).FirstOrDefaultAsync();
            Xunit.Assert.Null(resultMovie);
        }


        [Xunit.Theory]
        [Xunit.InlineData(null)]
        [Xunit.InlineData("")]
        public async Task DeleteAsync_WhenTitleIsNull_ShouldThrowArgumentException(string invalidName)
        {
            // Act and Assert
            Assert.ThrowsAsync<ArgumentException>(() => _controller.DeleteAsync(invalidName));
        }

        [Fact]
        public async Task DeleteAsync_WhenTitleIsEmpty_ShouldThrowArgumentException()
        {
            // Act and Assert            
        }

        [Fact]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            // Act and Assert
            Xunit.Assert.ThrowsAsync<InvalidOperationException>(() => _controller.DeleteAsync("Invalid movie title."));
        }

        [Fact]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Xunit.Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_WhenMoviesExist_ShouldReturnAllMovies()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Naruto Shipuuden",
                Director = "Denis Atanassov",
                YearReleased = 2022,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };       
            await _controller.AddAsync(movie);
            var movieTwo = new Movie
            {
                Title = "Sasuke",
                Director = "Denis Atanassov",
                YearReleased = 2022,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            await _controller.AddAsync(movieTwo);
            // Act
            var result1 = await _dbContext.Movies.Find(x => x.Title == movie.Title).FirstOrDefaultAsync();
            var result2 = await _dbContext.Movies.Find(y => y.Title == movieTwo.Title).FirstOrDefaultAsync();

            // Assert

            // Ensure that all movies are returned
            Xunit.Assert.NotNull(result1);
            Xunit.Assert.NotNull(result2);
            Xunit.Assert.Equal(movie.Title, result1.Title);
            Xunit.Assert.Equal(movieTwo.Title, result2.Title);
        }

        [Fact]
        public async Task GetByTitle_WhenTitleExists_ShouldReturnMatchingMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Naruto Shipuuden",
                Director = "Denis Atanassov",
                YearReleased = 2022,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            await _controller.AddAsync(movie);
            // Act
            var result = await _controller.GetByTitle(movie.Title);
            // Assert
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal(movie.Title, result.Title);
            Xunit.Assert.Equal(movie.Director, result.Director);
            Xunit.Assert.Equal(movie.YearReleased, result.YearReleased);
            Xunit.Assert.Equal(movie.Duration, result.Duration);
            Xunit.Assert.Equal(movie.Rating, result.Rating);
        }

        [Fact]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act
            var result = await _controller.GetByTitle("Non existing title");
            // Assert
            Xunit.Assert.Null(result);
        }


        [Fact]
        public async Task SearchByTitleFragmentAsync_WhenTitleFragmentExists_ShouldReturnMatchingMovies()
        {
            var movie = new Movie
            {
                Title = "Sasuke",
                Director = "Denis Atanassov",
                YearReleased = 1994,
                Genre = "Anime",
                Duration = 90,
                Rating = 8.5
            };
            await _controller.AddAsync(movie);


            // Act
            var result = await _controller.SearchByTitleFragmentAsync("Sa");

            // Assert // Should return one matching movie
            Xunit.Assert.NotEmpty(result);
            Xunit.Assert.Equal(1, result.Count());

            var resultMovie = result.First();
            Xunit.Assert.Equal(movie.Title, resultMovie.Title);
            Xunit.Assert.Equal(movie.Director, resultMovie.Director);
            Xunit.Assert.Equal(movie.YearReleased, resultMovie.YearReleased);
            Xunit.Assert.Equal(movie.Genre, resultMovie.Genre);
            Xunit.Assert.Equal(movie.Duration, resultMovie.Duration);
            Xunit.Assert.Equal(movie.Rating, resultMovie.Rating);
        }

        [Fact]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            Xunit.Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.SearchByTitleFragmentAsync("NonExistingMovieFragment"));

        }

        [Fact]
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

            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal(movieToUpdate.Title, result.Title);
            Xunit.Assert.Equal(movieToUpdate.Rating, result.Rating);
        }

        [Fact]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            // Movie without required fields
            var invalidMovie = new Movie
            {
                Rating = 0,
            };
            // Act and Assert
            Xunit.Assert.ThrowsAsync<ValidationException>(() => _controller.UpdateAsync(invalidMovie));
        }
    }
}
