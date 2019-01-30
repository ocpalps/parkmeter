// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Parkmeter
{
	/// <summary>
	/// The Startup class configures services and the request pipeline.
	/// </summary>
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			var builder = new ConfigurationBuilder()
				.SetBasePath(env.ContentRootPath)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
				.AddEnvironmentVariables();

			Configuration = builder.Build();
		}

		/// <summary>
		/// Gets the configuration that represents a set of key/value application configuration properties.
		/// </summary>
		/// <value>
		/// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
		/// </value>
		public IConfiguration Configuration { get; }

		/// <summary>
		/// This method gets called by the runtime. Use this method to add services to the container.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
		/// <seealso cref="IStatePropertyAccessor{T}"/>
		/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
		/// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
		public void ConfigureServices(IServiceCollection services)
		{
			services.AddBot<ParkmeterBot>(options =>
		   {
			   var secretKey = Configuration.GetSection("botFileSecret")?.Value;

				// Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
				var botConfig = BotConfiguration.Load(@".\Parkmeter.bot", secretKey);
			   services.AddSingleton(sp => botConfig);

				// Retrieve current endpoint.
				var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == "development").FirstOrDefault();
			   if (!(service is EndpointService endpointService))
			   {
				   throw new InvalidOperationException($"The .bot file does not contain a development endpoint.");
			   }

			   options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

				// Catches any errors that occur during a conversation turn and logs them.
				options.OnTurnError = async (context, exception) =>
			   {
				   await context.SendActivityAsync("Sorry, it looks like something went wrong.");
			   };

			   //TODO: Step 1 -> adding memory, conversation state and user state
			   IStorage dataStore = new MemoryStorage();

			   var conversationState = new ConversationState(dataStore);
			   options.State.Add(conversationState);

			   // Create and add user state.
			   var userState = new UserState(dataStore);
			   options.State.Add(userState);

		   });

			//TODO: Step 4 -> Dependency injection
			// Create and register state accessors.
			// Accessors created here are passed into the IBot-derived class on every turn.
			services.AddSingleton<ParkmeterAccessors>(sp =>
			{
				// We need to grab the conversationState we added on the options in the previous step
				var options = sp.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
				var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();
				var userState = options.State.OfType<UserState>().FirstOrDefault();

				// Create the custom state accessor.
				// State accessors enable other components to read and write individual properties of state.
				var accessors = new ParkmeterAccessors(conversationState, userState)
				{
					ConversationDialogState = conversationState.CreateProperty<DialogState>("DialogState"),
					UserProfile = userState.CreateProperty<UserProfile>("UserProfile"),
				};

				return accessors;
			});

		}

		public void Configure(IApplicationBuilder app, IHostingEnvironment env)
		{
			app.UseDefaultFiles()
				.UseStaticFiles()
				.UseBotFramework();
		}
	}
}
