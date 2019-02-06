using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Parkmeter
{
	//TODO: Step 3 -> Create ParkmeterAccessor class and import Bot.Builder.Dialogs as nuget package
	//Install-Package Microsoft.Bot.Builder.Dialogs -Version 4.2.0

	public class ParkmeterAccessors
	{
		public ParkmeterAccessors(ConversationState conversationState, UserState userState)
		{
			ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
			UserState = userState ?? throw new ArgumentNullException(nameof(userState));
		}

		public IStatePropertyAccessor<DialogState> ConversationDialogState { get; set; }
		public IStatePropertyAccessor<UserProfile> UserProfile { get; set; }

		public ConversationState ConversationState { get; }
		public UserState UserState { get; }

	}
}
