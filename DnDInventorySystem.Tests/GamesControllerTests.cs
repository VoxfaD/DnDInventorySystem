using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DnDInventorySystem;
using DnDInventorySystem.Controllers;
using DnDInventorySystem.Data;
using DnDInventorySystem.Models;
using DnDInventorySystem.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DnDInventorySystem.Tests
{
    public class GamesControllerTests
    {
        private readonly ITestOutputHelper _output;

        public GamesControllerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // Spēles izveides testi 
        [Fact]
        public async Task CreateGame_Succeeds_WithValidName()
        {
            var (controller, context, userId) = BuildController();
            var model = new Game { Name = "Lietains" };

            var result = await controller.Create(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Single(context.Games);
            var saved = context.Games.Include(g => g.UserGameRoles).Single();
            Assert.Equal("Lietains", saved.Name);
            Assert.Equal(userId, saved.CreatedByUserId);
            var ownerRole = Assert.Single(saved.UserGameRoles);
            Assert.True(ownerRole.IsOwner);
            Assert.Equal(userId, ownerRole.UserId);
            Assert.Equal("Game \"Lietains\" created.", controller.TempData["GameMessage"]);
            _output.WriteLine(Translate(controller.TempData["GameMessage"]?.ToString() ?? string.Empty));
        }

        [Fact]
        public async Task CreateGame_Fails_WhenNameEmpty()
        {
            var (controller, context, _) = BuildController();
            var model = new Game { Name = "" };

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
            Assert.Contains(controller.ModelState[nameof(model.Name)]!.Errors,
                e => e.ErrorMessage == "Please fill in the game name!");
            Assert.Empty(context.Games);
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task CreateGame_Fails_WhenNameTooLong()
        {
            var (controller, context, _) = BuildController();
            var model = new Game { Name = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" };

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Name)));
            Assert.Contains(controller.ModelState[nameof(model.Name)]!.Errors,
                e => e.ErrorMessage == "Character limit exceeded for the name!");
            Assert.Empty(context.Games);
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task CreateGame_Fails_WhenDescriptionTooLong()
        {
            var (controller, context, _) = BuildController();
            var model = new Game
            {
                Name = "Lietains",
                Description = new string('b', 2001)
            };

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.Description)));
            Assert.Contains(controller.ModelState[nameof(model.Description)]!.Errors,
                e => e.ErrorMessage == "Character limit exceeded for the description!");
            Assert.Empty(context.Games);
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        // Pievienošanās koda testi 
        [Fact]
        public async Task GenerateJoinCode_Succeeds_ForOwner()
        {
            var (controller, context, userId) = BuildController();
            var game = new Game { Name = "Joinable", CreatedByUserId = userId };
            game.UserGameRoles.Add(new UserGameRole { UserId = userId, IsOwner = true, Privileges = PrivilegeSets.Owner, PrivilegesNames = PrivilegeSets.ToNames(PrivilegeSets.Owner) });
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var result = await controller.GenerateJoinCode(game.Id);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("JoinCode", redirect.ActionName);
            var saved = context.Games.Single();
            Assert.True(saved.JoinCodeActive);
            Assert.False(string.IsNullOrWhiteSpace(saved.JoinCode));
            Assert.Equal("Join code generated and activated.", controller.TempData["GameMessage"]);
            _output.WriteLine(Translate(controller.TempData["GameMessage"]?.ToString() ?? string.Empty));
        }

        [Fact]
        public async Task Join_Succeeds_WithActiveCode()
        {
            var (controller, context, userId) = BuildController();
            var game = new Game
            {
                Name = "Lietains",
                CreatedByUserId = 999,
                JoinCode = "ABC-1234-5678",
                JoinCodeActive = true
            };
            game.UserGameRoles.Add(new UserGameRole { UserId = 999, IsOwner = true, Privileges = PrivilegeSets.Owner, PrivilegesNames = PrivilegeSets.ToNames(PrivilegeSets.Owner) });
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var model = new JoinGameViewModel { JoinCode = "abc-1234-5678" };

            var result = await controller.Join(model);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            var membership = context.UserGameRoles.Single(r => r.UserId == userId && r.GameId == game.Id);
            Assert.False(membership.IsOwner);
            Assert.Equal(PrivilegeSets.Player, membership.Privileges);
            Assert.Equal($"You joined \"{game.Name}\".", controller.TempData["GameMessage"]);
            _output.WriteLine(Translate(controller.TempData["GameMessage"]?.ToString() ?? string.Empty));
        }

        [Fact]
        public async Task Join_Fails_WhenCodeDeactivated()
        {
            var (controller, context, userId) = BuildController();
            var game = new Game
            {
                Name = "Lietains",
                CreatedByUserId = 999,
                JoinCode = "ABC-1234-5678",
                JoinCodeActive = false
            };
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var model = new JoinGameViewModel { JoinCode = "ABC-1234-5678" };

            var result = await controller.Join(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.JoinCode)));
            Assert.Contains(controller.ModelState[nameof(model.JoinCode)]!.Errors,
                e => e.ErrorMessage == "Invitation code not found or not active!");
            Assert.Empty(context.UserGameRoles.Where(r => r.UserId == userId));
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Join_Fails_WithInvalidCode()
        {
            var (controller, context, userId) = BuildController();
            var model = new JoinGameViewModel { JoinCode = "ABC-1234-5" };

            var result = await controller.Join(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.JoinCode)));
            Assert.Contains(controller.ModelState[nameof(model.JoinCode)]!.Errors,
                e => e.ErrorMessage == "Invitation code not found or not active!");
            Assert.Empty(context.UserGameRoles.Where(r => r.UserId == userId));
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        [Fact]
        public async Task Join_Fails_WhenAlreadyMember()
        {
            var (controller, context, userId) = BuildController();
            var game = new Game
            {
                Name = "Lietains",
                CreatedByUserId = 999,
                JoinCode = "ABC-1234-5678",
                JoinCodeActive = true
            };
            game.UserGameRoles.Add(new UserGameRole { UserId = userId, IsOwner = false, Privileges = PrivilegeSets.Player, PrivilegesNames = PrivilegeSets.ToNames(PrivilegeSets.Player) });
            context.Games.Add(game);
            await context.SaveChangesAsync();

            var model = new JoinGameViewModel { JoinCode = "ABC-1234-5678" };

            var result = await controller.Join(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey(nameof(model.JoinCode)));
            Assert.Contains(controller.ModelState[nameof(model.JoinCode)]!.Errors,
                e => e.ErrorMessage == "You have already joined this game!");
            Assert.Equal(1, context.UserGameRoles.Count(r => r.UserId == userId && r.GameId == game.Id));
            Assert.Same(model, viewResult.Model);
            LogTranslatedErrors(controller);
        }

        private static (GamesController controller, ApplicationDbContext context, int userId) BuildController()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            var serviceProvider = services.BuildServiceProvider();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"games-{System.Guid.NewGuid()}")
                .Options;
            var context = new ApplicationDbContext(options);
            var historyLog = new HistoryLogService(context);

            var userId = 123;
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, "TestUser")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                RequestServices = serviceProvider,
                User = claimsPrincipal
            };

            var actionContext = new ActionContext(httpContext, new RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            var controller = new GamesController(context, historyLog)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, new DictionaryTempDataProvider()),
                Url = new UrlHelper(actionContext)
            };

            return (controller, context, userId);
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
            "Please fill in the game name!" => "Ludzu aizpildiet speles nosaukumu!",
            "Character limit exceeded for the name!" => "Parsniedz simbolu skaitu nosaukumam!",
            "Character limit exceeded for the description!" => "Parsniedz simbolu skaitu aprakstam!",
            "Game \"Lietains\" created." => "Spele \"Lietains\" izveidota.",
            "Join code generated and activated." => "Pievienošanas kods ģenerēts un aktivizēts.",
            "Invitation code not found or not active!" => "Pievienošanas kods nav atrasts vai nav aktīvs!",
            "You have already joined this game!" => "Jūs jau esat pievienojies šai spēlei!",
            "You joined \"Lietains\"." => "Jūs pievienojāties \"Lietains\".",
            _ => message
        };

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
