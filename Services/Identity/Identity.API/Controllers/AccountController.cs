﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AutoMapper;
using Identity.API.Data;
using Identity.API.IntegrationEvents;
using Identity.API.IntegrationEvents.Event;
using Identity.API.Models;
using Identity.API.Models.Dtos;
using Identity.API.Services;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopOnContainers.Services.Identity.API.Models.AccountViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Identity.API.Controllers
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    public class AccountController : Controller
    {
        //private readonly InMemoryUserLoginService _loginService;
        private readonly ILoginService<AppUser> _loginService;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private IMapper _mapper;
        private readonly IIdentityntegrationEventService _IIdentityntegrationEventService;

        public AccountController(

            //InMemoryUserLoginService loginService,
            ILoginService<AppUser> loginService,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            ILogger<AccountController> logger,
            UserManager<AppUser> userManager,
            IConfiguration configuration, 
            SignInManager<AppUser> signInManager,
            ApplicationDbContext context, IMapper mapper,
            IIdentityntegrationEventService IIdentityntegrationEventService)
        {
            _loginService = loginService;
            _interaction = interaction;
            _clientStore = clientStore;
            _logger = logger;
            _userManager = userManager;
            _configuration = configuration;
            _signInManager = signInManager;
            _context = context;
            _mapper = mapper;
            _IIdentityntegrationEventService = IIdentityntegrationEventService;

        }

        /// <summary>
        /// Show login page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Login(string returnUrl)
        {
            if (HttpContext.User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null)
            {
                throw new NotImplementedException("External login is not implemented!");
            }

            var vm = await BuildLoginViewModelAsync(returnUrl, context);

            ViewData["ReturnUrl"] = returnUrl;

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _loginService.FindByUsername(model.Email);

                if (await _loginService.ValidateCredentials(user, model.Password))
                {
                    var tokenLifetime = _configuration.GetValue("TokenLifetimeMinutes", 120);

                    var props = new AuthenticationProperties
                    {
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(tokenLifetime),
                        AllowRefresh = true,
                        RedirectUri = model.ReturnUrl
                    };

                    if (model.RememberMe)
                    {
                        var permanentTokenLifetime = _configuration.GetValue("PermanentTokenLifetimeDays", 365);

                        props.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(permanentTokenLifetime);
                        props.IsPersistent = true;
                    };

                    await _loginService.SignInAsync(user, props);

                    // make sure the returnUrl is still valid, and if yes - redirect back to authorize endpoint
                    if (_interaction.IsValidReturnUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }

                    return Redirect("~/");
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model);

            ViewData["ReturnUrl"] = model.ReturnUrl;

            return View(vm);
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, AuthorizationRequest context)
        {
            var allowLocal = true;
            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;
                }
            }

            return new LoginViewModel
            {
                ReturnUrl = returnUrl,
                Email = context?.LoginHint,
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl, context);
            vm.Email = model.Email;
            vm.RememberMe = model.RememberMe;
            return vm;
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                // if the user is not authenticated, then just show logged out page
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            //Test for Xamarin. 
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                //it's safe to automatically sign-out
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            var vm = new LogoutViewModel
            {
                LogoutId = logoutId
            };
            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
            {
                if (model.LogoutId == null)
                {
                    // if there's no current logout context, we need to create one
                    // this captures necessary info from the current logged in user
                    // before we signout and redirect away to the external IdP for signout
                    model.LogoutId = await _interaction.CreateLogoutContextAsync();
                }

                string url = "/Account/Logout?logoutId=" + model.LogoutId;

                try
                {

                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = url
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LOGOUT ERROR: {ExceptionMessage}", ex.Message);
                }
            }

            // delete authentication cookie
            await HttpContext.SignOutAsync();

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            return Redirect(logout?.PostLogoutRedirectUri);
        }

        public async Task<IActionResult> DeviceLogOut(string redirectUrl)
        {
            // delete authentication cookie
            await HttpContext.SignOutAsync();

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return Redirect(redirectUrl);
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        
        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                //** Verify if email is registered
                if (await _loginService.FindByUsername(model.Email) != null)
                    return BadRequest("This email is already registered. Please use a different email.");

                //** Register User
                //var Identityuser = _mapper.Map<AppUser>(registerDto);
                var user = new AppUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.User.FirstName,
                    LastName = model.User.LastName
                };

                //var result = await _loginService.CreateUser(user, model.Password);
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Errors.Count() > 0)
                {
                    AddErrors(result);
                    // If we got this far, something failed, redisplay form
                    return View(model);
                }
                
                //** Add Role
                var roleAssigned = await _userManager.AddToRoleAsync(user, "Customer");
                
                //** Add Customer
                await _context.Customers.AddAsync(new Customer { IdentityId = user.Id, Gender = model.Gender });
                await _context.SaveChangesAsync();
            }

            if (returnUrl != null)
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                    return Redirect(returnUrl);
                else
                    if (ModelState.IsValid)
                    return RedirectToAction("login", "account", new { returnUrl = returnUrl });
                else
                    return View(model);
            }

            return RedirectToAction("index", "home");
        }
        

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginViewModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl, context);
            vm.Email = model.Email;
            vm.RememberMe = model.RememberMe;
            return vm;
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Logout(string logoutId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                // if the user is not authenticated, then just show logged out page
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            //Test for Xamarin. 
            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                //it's safe to automatically sign-out
                return await Logout(new LogoutViewModel { LogoutId = logoutId });
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            var vm = new LogoutViewModel
            {
                LogoutId = logoutId
            };
            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutViewModel model)
        {
            var idp = User?.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;

            if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
            {
                if (model.LogoutId == null)
                {
                    // if there's no current logout context, we need to create one
                    // this captures necessary info from the current logged in user
                    // before we signout and redirect away to the external IdP for signout
                    model.LogoutId = await _interaction.CreateLogoutContextAsync();
                }

                string url = "/Account/Logout?logoutId=" + model.LogoutId;

                try
                {

                    // hack: try/catch to handle social providers that throw
                    await HttpContext.SignOutAsync(idp, new AuthenticationProperties
                    {
                        RedirectUri = url
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LOGOUT ERROR: {ExceptionMessage}", ex.Message);
                }
            }

            // delete authentication cookie
            await HttpContext.SignOutAsync();

            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(model.LogoutId);

            return Redirect(logout?.PostLogoutRedirectUri);
        }

        public async Task<IActionResult> DeviceLogOut(string redirectUrl)
        {
            // delete authentication cookie
            await HttpContext.SignOutAsync();

            // set this so UI rendering sees an anonymous user
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return Redirect(redirectUrl);
        }

        // GET: /Account/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /*
        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    CardHolderName = model.User.CardHolderName,
                    CardNumber = model.User.CardNumber,
                    CardType = model.User.CardType,
                    City = model.User.City,
                    Country = model.User.Country,
                    Expiration = model.User.Expiration,
                    LastName = model.User.LastName,
                    Name = model.User.Name,
                    Street = model.User.Street,
                    State = model.User.State,
                    ZipCode = model.User.ZipCode,
                    PhoneNumber = model.User.PhoneNumber,
                    SecurityNumber = model.User.SecurityNumber
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Errors.Count() > 0)
                {
                    AddErrors(result);
                    // If we got this far, something failed, redisplay form
                    return View(model);
                }
            }

            if (returnUrl != null)
            {
                if (HttpContext.User.Identity.IsAuthenticated)
                    return Redirect(returnUrl);
                else
                    if (ModelState.IsValid)
                    return RedirectToAction("login", "account", new { returnUrl = returnUrl });
                else
                    return View(model);
            }

            return RedirectToAction("index", "home");
        }
        */

        //
    // POST: /Account/Profile
    /// <summary>
    /// Action to update profile information
    /// </summary>
    /// <param name="profileDto">Model to update customer</param>
    /// <returns>Returns Succcess message</returns>
    [HttpPost("UpdateProfile")]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileDto profileDto)
    {
        if (ModelState.IsValid)
        {
            //** Verify if email is registered
            var user = await _userManager.FindByEmailAsync(profileDto.Email.ToLower());

            if (user == null)
                return BadRequest("No user found.");

            //var user = _mapper.Map<userFound>();
            _mapper.Map(profileDto, user);

            //** Update User
            await _userManager.UpdateAsync(user);
            
            //** Get customer id
            var customerId = _context.Customers.AsNoTracking().Where(c => c.IdentityId == user.Id).FirstOrDefault().Id;
            
            //** Update Customer
            var customer = new Customer 
            { 
                IdentityId = user.Id,
                Gender = profileDto.Gender,
                Id = customerId
            };

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            //Create Integration Event to be published through the Event Bus
            var customerDetailsChangedEvent = new CustomerDetailsChangedIntegrationEvent(customer.Id, user.FirstName + ' ' + user.LastName, user.PhoneNumber);

            // Achieving atomicity between original Identity database operation and the IntegrationEventLog with a local transaction
            await _IIdentityntegrationEventService.SaveEventAndIdentityContextChangesAsync(customerDetailsChangedEvent);

            // Publish through the Event Bus and mark the saved event as published
            await _IIdentityntegrationEventService.PublishThroughEventBusAsync(customerDetailsChangedEvent);

        }
        else
        {
            return BadRequest(ModelState);
        }

        return Ok("You have successfully Updated profile....");
    }

        [HttpGet]
        public IActionResult Redirecting()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

    }

            if (user == null)
                return BadRequest("No user found.");

            //var user = _mapper.Map<userFound>();
            _mapper.Map(profileDto, user);

            //** Update User
            await _userManager.UpdateAsync(user);
            
            //** Get customer id
            var customerId = _context.Customers.AsNoTracking().Where(c => c.IdentityId == user.Id).FirstOrDefault().Id;
            
            //** Update Customer
            var customer = new Customer 
            { 
                IdentityId = user.Id,
                Gender = profileDto.Gender,
                Id = customerId
            };

            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            //Create Integration Event to be published through the Event Bus
            var customerDetailsChangedEvent = new CustomerDetailsChangedIntegrationEvent(customer.Id, user.FirstName + ' ' + user.LastName, user.PhoneNumber);

            // Achieving atomicity between original Identity database operation and the IntegrationEventLog with a local transaction
            await _IIdentityntegrationEventService.SaveEventAndIdentityContextChangesAsync(customerDetailsChangedEvent);

            // Publish through the Event Bus and mark the saved event as published
            await _IIdentityntegrationEventService.PublishThroughEventBusAsync(customerDetailsChangedEvent);

        }
        else
        {
            return BadRequest(ModelState);
        }

        return Ok("You have successfully Updated profile....");
    }

        [HttpGet]
        public IActionResult Redirecting()
        {
            return View();
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

    }
}