using HospitalInfo.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HopitalInfo.Dialogs
{
    public class MainDialog: ComponentDialog
    {
        private readonly HospitalInfoRecognizer _luisRecognizer;
        protected readonly ILogger Logger;
        private BotState _userState;
        public MainDialog(HospitalInfoRecognizer luisRecognizer, UserState userState,HospitalFindingDialog hospitalFindingDialog,LocalisationFindingDialog localisationFindingDialog,HospitalCaracteristicsFindingDialog hospitalCaracteristicsFindingDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _luisRecognizer = luisRecognizer;
            Logger = logger;
            _userState = userState;
            // The initial child Dialog to run.
            InitialDialogId = nameof(MainDialog);
            
            //Add Dialogs
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(hospitalFindingDialog);
            AddDialog(localisationFindingDialog);
            AddDialog(hospitalCaracteristicsFindingDialog);
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? "Que puis je faire pour vous?";
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
            
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call LUIS and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var luisResult = await _luisRecognizer.RecognizeAsync<HospitalInfoService>(stepContext.Context, cancellationToken);

            switch (luisResult.TopIntent().intent)
            {
                case HospitalInfoService.Intent.HospitalFinding:
                    var localisation = luisResult.Entities.Localisation;
                    //Lancement du Dialogue permettant de retrouver le nom d'un Hôpital
                    return await stepContext.BeginDialogAsync(nameof(HospitalFindingDialog), localisation, cancellationToken);
                case HospitalInfoService.Intent.LocalisationFinding:
                    var hospitalName = luisResult.Entities.Nom_Hopital;
                    //Lancement du Dialogue permettant de retrouver la localisation (province ou commune) d'un Hôpital
                    return await stepContext.BeginDialogAsync(nameof(LocalisationFindingDialog), hospitalName, cancellationToken);
                case HospitalInfoService.Intent.HospitalCaracteristicsFinding:
                    var hospital = luisResult.Entities.Nom_Hopital;
                    //Lancement du Dialogue permettant d'identifier les caractéristiques complètes d'un Hôpital
                    return await stepContext.BeginDialogAsync(nameof(HospitalCaracteristicsFindingDialog), hospital, cancellationToken);
                default:
                    return await stepContext.NextAsync();
            }
            
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
                var messageText = $"Merci de nous avoir consulté";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            

            // Redémarage du dialogue principal
            var promptMessage = "Que puis je encore faire pour vous?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
