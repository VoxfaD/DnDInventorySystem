using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using DnDInventorySystem.Controllers;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using DnDInventorySystem.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DnDInventorySystem.Tests
{
    public class AccountControllerTests
    {
        private readonly ITestOutputHelper _output;

        public AccountControllerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // Reģistrācijas testi
        [Fact]
        public async Task Register_Succeeds_WithValidInput()
        {
            var (controller, context) = BuildController();
            var model = new RegisterViewModel
            {
                Name = "Linda",
                Email = "linda@mail.com",
                Password = "Parole123!",
                ConfirmPassword = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Games", redirect.ControllerName);
            Assert.Single(context.Users);
            Assert.Equal("Linda", context.Users.First().Name);
            _output.WriteLine("Reģistrācija veiksmīga!");
        }

        [Fact]
        public async Task Register_Fails_WhenFieldsEmpty()
        {
            var (controller, _) = BuildController();
            var model = new RegisterViewModel
            {
                Name = "",
                Email = "",
                Password = "",
                ConfirmPassword = ""
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Password)));
            Assert.True(controller.ModelState.ContainsKey(nameof(model.ConfirmPassword)));
            Assert.Contains(controller.ModelState[nameof(model.Name)]!.Errors,
                e => e.ErrorMessage == "Username is required!");
            Assert.Contains(controller.ModelState[nameof(model.Email)]!.Errors,
                e => e.ErrorMessage == "E-mail is required!");
            Assert.Contains(controller.ModelState[nameof(model.Password)]!.Errors,
                e => e.ErrorMessage == "Password is required!");
            Assert.Contains(controller.ModelState[nameof(model.ConfirmPassword)]!.Errors,
                e => e.ErrorMessage == "Password confirmation is required!");
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Register_Fails_WhenUsernameTooLong()
        {
            var (controller, _) = BuildController();
            var model = new RegisterViewModel
            {
                Name = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                Email = "alice@example.com",
                Password = "Parole123!",
                ConfirmPassword = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
            Assert.Contains(controller.ModelState[nameof(model.Name)]!.Errors,
                e => e.ErrorMessage == "user does not match the specified number of symbols 1-50 symbols!");
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

                [Fact]
        public async Task Register_Fails_WhenEmailAlreadyExists()
        {
            var (controller, context) = BuildController();
            context.Users.Add(new User { Name = "Linda", Email = "linda@mail.com", PasswordHash = "hash" });
            await context.SaveChangesAsync();

            var model = new RegisterViewModel
            {
                Name = "Linda2",
                Email = "linda@mail.com",
                Password = "Parole123!",
                ConfirmPassword = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
            Assert.Contains(controller.ModelState[nameof(model.Email)]!.Errors,
                e => e.ErrorMessage == "This email address is already registered!");
            Assert.Same(model, viewResult.Model);
            Assert.Equal(1, context.Users.Count());
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Register_Fails_WhenUsernameAlreadyExists()
        {
            var (controller, context) = BuildController();
            context.Users.Add(new User { Name = "Tester", Email = "tester@mail.com", PasswordHash = "hash" });
            await context.SaveChangesAsync();

            var model = new RegisterViewModel
            {
                Name = "Tester",
                Email = "tester2@mail.com",
                Password = "Parole123!",
                ConfirmPassword = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
            Assert.Contains(controller.ModelState[nameof(model.Name)]!.Errors,
                e => e.ErrorMessage == "Username already exists!");
            Assert.Same(model, viewResult.Model);
            Assert.Equal(1, context.Users.Count());
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Register_Fails_WithInvalidEmail()
        {
            var (controller, _) = BuildController();
            var model = new RegisterViewModel
            {
                Name = "Linda",
                Email = "slikts",
                Password = "Parole123!",
                ConfirmPassword = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Email)));
            Assert.Contains(controller.ModelState[nameof(model.Email)]!.Errors,
                e => e.ErrorMessage == "Email address does not match the format!");
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Register_Fails_WhenPasswordTooShort()
        {
            var (controller, _) = BuildController();
            var model = new RegisterViewModel
            {
                Name = "Linda",
                Email = "linda@example.com",
                Password = "mazs",
                ConfirmPassword = "mazs"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Password)));
            Assert.Contains(controller.ModelState[nameof(model.Password)]!.Errors,
                e => e.ErrorMessage == "Password does not meet security requirements! Must contain a number and a unique symbol, such as #, must be at least an uppercase letter and the number of symbols is 8-128 symbols.");
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Register_Fails_WhenPasswordsDoNotMatch()
        {
            var (controller, _) = BuildController();
            var model = new RegisterViewModel
            {
                Name = "Linda",
                Email = "linda@example.com",
                Password = "Parole123!",
                ConfirmPassword = "Parole890&"
            };

            ValidateModel(controller, model);

            var result = await controller.Register(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.ConfirmPassword)));
            Assert.Contains(controller.ModelState[nameof(model.ConfirmPassword)]!.Errors,
                e => e.ErrorMessage == "Password fields do not match!");
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }



        // Autentifikācijas testi 
        [Fact]
        public async Task Login_Succeeds_WithValidCredentials()
        {
            var (controller, context) = BuildController();
            var user = new User { Name = "Exist", Email = "exist@mail.com" };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Parole123!");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = "exist@mail.com",
                Password = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Login(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Games", redirect.ControllerName);
            Assert.Single(context.Users);
            _output.WriteLine("Autentifikācija veiksmīga!");
        }

        [Fact]
        public async Task Login_Fails_WithIncorrectPassword()
        {
            var (controller, context) = BuildController();
            var user = new User { Name = "Exist", Email = "exist@mail.com" };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Parole123!");
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var model = new LoginViewModel
            {
                Email = "exist@mail.com",
                Password = "Parole890&"
            };

            ValidateModel(controller, model);

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Contains(controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Incorrect email address or password!");
            Assert.Same(model, viewResult.Model);
            Assert.Single(context.Users);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Login_Fails_WithUnknownEmail()
        {
            var (controller, context) = BuildController();
            var model = new LoginViewModel
            {
                Email = "nav@mail.com",
                Password = "Parole123!"
            };

            ValidateModel(controller, model);

            var result = await controller.Login(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(string.Empty));
            Assert.Contains(controller.ModelState[string.Empty]!.Errors,
                e => e.ErrorMessage == "Incorrect email address or password!");
            Assert.Same(model, viewResult.Model);
            Assert.Empty(context.Users);
            LogTranslatedErrors(controller);
        }




        private static (AccountController controller, ApplicationDbContext context) BuildController()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IAuthenticationService>(new NoOpAuthService());
            services.AddSingleton<ITempDataDictionaryFactory, TestTempDataFactory>();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            var serviceProvider = services.BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"users-{System.Guid.NewGuid()}")
                .Options;
            var context = new ApplicationDbContext(options);
            var passwordHasher = new PasswordHasher<User>();

            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            var tempDataFactory = serviceProvider.GetRequiredService<ITempDataDictionaryFactory>();
            var controller = new AccountController(context, passwordHasher)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = tempDataFactory.GetTempData(httpContext),
                Url = new UrlHelper(actionContext)
            };

            return (controller, context);
        }

        private static void ValidateModel(Controller controller, object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, validationResults, validateAllProperties: true);

            foreach (var validationResult in validationResults)
            {
                var memberName = validationResult.MemberNames.FirstOrDefault() ?? string.Empty;
                controller.ModelState.AddModelError(memberName, validationResult.ErrorMessage ?? "Validation error");
            }
        }

        private void LogTranslatedErrors(Controller controller)
        {
            foreach (var kvp in controller.ModelState)
            {
                foreach (var error in kvp.Value.Errors)
                {
                    var translated = Translate(error.ErrorMessage);
                    _output.WriteLine($"{kvp.Key}: {translated}");
                }
            }
        }

        private static string Translate(string message) => message switch
        {
            "Username is required!" => "Lietotājvārds ir obligāts!",
            "E-mail is required!" => "E-pasts ir obligāts!",
            "Password is required!" => "Parole ir obligāta!",
            "Password confirmation is required!" => "Paroles apstiprinājums ir obligāts!",
            "Email address does not match the format!" => "E-pasta adrese neatbilst formātam!",
            "email does not match the specified number of symbols 5-100 symbols!" => "E-pastam jābūt 5-100 simboli!",
            "This email address is already registered!" => "Šī e-pasta adrese jau ir reģistrēta!",
            "Username already exists!" => "Lietotājvārds jau eksistē!",
            "user does not match the specified number of symbols 1-50 symbols!" => "Lietotājvārdam jābūt 1-50 simboli!",
            "Password does not meet security requirements! Must contain a number and a unique symbol, such as #, must be at least an uppercase letter and the number of symbols is 8-128 symbols." =>
            "Parole neatbilst drošības prasībām! Jābūt skaitlim, simbolam (piemēram, #), lielajam burtam un 8-128 simboliem.",
            "Password fields do not match!" => "Paroles lauki nesakrīt!",
            "Incorrect email address or password!" => "Nepareiza E-pasta adrese vai parole!",
            _ => message
        };

        private class NoOpAuthService : IAuthenticationService
        {
            public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme) =>
                Task.FromResult(AuthenticateResult.NoResult());

            public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties? properties) =>
                Task.CompletedTask;

            public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties? properties) =>
                Task.CompletedTask;

            public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
                Task.CompletedTask;

            public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
                Task.CompletedTask;
        }

        private class TestTempDataFactory : ITempDataDictionaryFactory
        {
            private readonly ITempDataProvider _provider = new DictionaryTempDataProvider();

            public ITempDataDictionary GetTempData(HttpContext context) =>
                new TempDataDictionary(context, _provider);

        }

        private class DictionaryTempDataProvider : ITempDataProvider
        {
            private readonly Dictionary<string, object> _store = new();

            public IDictionary<string, object> LoadTempData(HttpContext context) =>
                new Dictionary<string, object>(_store);

            public void SaveTempData(HttpContext context, IDictionary<string, object> values)
            {
                _store.Clear();
                foreach (var kvp in values)
                {
                    _store[kvp.Key] = kvp.Value;
                }
            }
        }
    }
}
