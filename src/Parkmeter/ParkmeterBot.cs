// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Parkmeter
{
	/// <summary>
	/// Represents a bot that processes incoming activities.
	/// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
	/// This is a Transient lifetime service. Transient lifetime services are created
	/// each time they're requested. Objects that are expensive to construct, or have a lifetime
	/// beyond a single turn, should be carefully managed.
	/// For example, the <see cref="MemoryStorage"/> object and associated
	/// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
	/// </summary>
	/// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
	public class ParkmeterBot : IBot
	{
		//TODO: Step 5 -> declare variables
		private readonly ParkmeterAccessors _accessors;
		private DialogSet _dialogs;

		/// <summary>
		/// Initializes a new instance of the class.
		/// </summary> 
		//TODO: Step 6 -> edit constructor
		//passing accessors to it and editing the logic
		public ParkmeterBot(ParkmeterAccessors accessors)
		{
			_accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));

			// The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
			_dialogs = new DialogSet(accessors.ConversationDialogState);

			// This array defines how the Waterfall will execute.
			var waterfallSteps = new WaterfallStep[]
			{
				NameStepAsync,
				PlateStepAsync,
				ConfirmStepAsync,
				SummaryStepAsync
			};

			// Add named dialogs to the DialogSet. These names are saved in the dialog state.
			_dialogs.Add(new WaterfallDialog("booking", waterfallSteps));
			_dialogs.Add(new TextPrompt("pname"));
			_dialogs.Add(new TextPrompt("plate"));
			_dialogs.Add(new ConfirmPrompt("confirm"));
		}

		/// <summary>
		/// Every conversation turn calls this method.
		/// </summary>
		/// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
		/// for processing this conversation turn. </param>
		/// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
		/// or threads to receive notice of cancellation.</param>
		/// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
		/// <seealso cref="BotStateSet"/>
		/// <seealso cref="ConversationState"/>
		public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Handle Message activity type, which is the main activity type for shown within a conversational interface
			// Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
			// see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
			if (turnContext.Activity.Type == ActivityTypes.Message)
			{
				//TODO: Step 8 -> implement the call to the dialog
				// Run the DialogSet - let the framework identify the current state of the dialog from
				// the dialog stack and figure out what (if any) is the active dialog.
				var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
				var results = await dialogContext.ContinueDialogAsync(cancellationToken);

				// If the DialogTurnStatus is Empty we should start a new dialog.
				if (results.Status == DialogTurnStatus.Empty)
				{
					await dialogContext.BeginDialogAsync("booking", null, cancellationToken);
				}
				else if (results.Status == DialogTurnStatus.Complete)
				{
					await turnContext.SendActivityAsync("Thanks! bye bye!");
				}
			}

			// Save the dialog state into the conversation state.
			await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

			// Save the user profile updates into the user state.
			await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
		}

		//TODO: Step 7 -> defining waterfall steps
		//TODO: Step 7a -> asking parking name
		private static async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			// WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
			// Running a prompt here means the next WaterfallStep will be run when the users response is received.
			return await stepContext.PromptAsync("pname", new PromptOptions { Prompt = MessageFactory.Text("Please enter the parking name.") }, cancellationToken);
		}

		//TODO: Step 7b -> storing answer and asking the licence plate
		private async Task<DialogTurnResult> PlateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			// We can send messages to the user at any point in the WaterfallStep.
			if ((string)stepContext.Result == null)
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"No parking name given."), cancellationToken);

			}
			else
			{
				// Get the current profile object from user state.
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// Update the profile.
				userProfile.ParkingName = (string)stepContext.Result;

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The parking name is {userProfile.ParkingName}."), cancellationToken);

			}

			return await stepContext.PromptAsync("plate", new PromptOptions { Prompt = MessageFactory.Text("Please enter your licence plate.") }, cancellationToken);

		}

		//TODO: Step 7c -> storing answer and preparing the summary to show the user
		private async Task<DialogTurnResult> ConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((string)stepContext.Result == null)
			{
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"No licence plate given."), cancellationToken);

			}
			else
			{
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// Update the profile.
				userProfile.LicencePlate = (string)stepContext.Result;

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The licence plate as {userProfile.LicencePlate}."), cancellationToken);
			}

			// WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is a Prompt Dialog.
			return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("Is this ok?") }, cancellationToken);
		}

		//TODO: Step 7d -> displaying the info
		private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
		{
			if ((bool)stepContext.Result)
			{
				// Get the current profile object from user state.
				var userProfile = await _accessors.UserProfile.GetAsync(stepContext.Context, () => new UserProfile(), cancellationToken);

				// TODO: eseguire la chiamata a CosmosDB per ritornare i dati

				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text($"I have booked a spot in the {userProfile.ParkingName} for the car with licence plate {userProfile.LicencePlate}."), cancellationToken);

			}
			else
			{
				// We can send messages to the user at any point in the WaterfallStep.
				await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thanks. Your profile will not be kept."), cancellationToken);
			}

			// WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
			return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
		}
	}
}