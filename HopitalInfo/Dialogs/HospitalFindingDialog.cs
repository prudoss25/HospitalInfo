using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HopitalInfo.Dialogs
{
    public class HospitalFindingDialog : CancelAndInfoDialog
    {
        private BotState _userState;
        private String localisation="";
        public HospitalFindingDialog(InfoCategorieDialog infoCategorieDialog,HospitalInfoRecognizer luisRecognizer,UserState userState)
            : base( luisRecognizer,nameof(HospitalFindingDialog),infoCategorieDialog)
        {
            InitialDialogId = nameof(HospitalFindingDialog);
            _userState = userState;
            AddDialog(new ChoicePrompt(DialogIds.validateLocalisationPrompt));
            AddDialog(new TextPrompt(DialogIds.localisationPrompt, VerificationLocalisation));
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                IntroHospitalFindingStep,
                ConfirmLocalisationStep,
                ShowHospitalStep,
                FinalHospitalFindingStep,
            }));
        }
        private async Task<DialogTurnResult> IntroHospitalFindingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DonneesExcel donneesExcel = new DonneesExcel();
            if(stepContext.Options!=null)
            {
                localisation = "";
                var localisationDetected = (String[])stepContext.Options;
                for (int i = 0; i < localisationDetected.Length; i++)
                    localisation += localisationDetected[i];
            }

            if (String.IsNullOrEmpty(localisation))
            {
                // Asking Localisation.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Veuillez entrer une province, préfecture ou ville."),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais la localisation entrée n'est pas enregistrée dans notre base de donnée. Veuillez s'il vous plaît entrer une localisation valide."),
                };
                return await stepContext.PromptAsync(DialogIds.localisationPrompt, promptOptions, cancellationToken);
            }
            else if(!donneesExcel.ExistLocalisationHopital(localisation))
            {
                // Asking Localisation.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text($"La localisation {localisation} n'existe pas dans notre base de donnée. Veuillez entrer une localisation valide"),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais la localisation entrée n'est pas enregistrée dans notre base de donnée. Veuillez s'il vous plaît entrer une localisation valide."),
                };
                return await stepContext.PromptAsync(DialogIds.localisationPrompt, promptOptions, cancellationToken);
            }

            return await stepContext.NextAsync(localisation, cancellationToken);


        }

       

        private async Task<DialogTurnResult> ConfirmLocalisationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            localisation = (string)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"La localisation entrée est \"{localisation}\"."), cancellationToken);

            
            return await stepContext.PromptAsync(DialogIds.validateLocalisationPrompt, new PromptOptions
            {
                Prompt = MessageFactory.Text("Etes vous sûr de votre choix ?"),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Oui", "Non" }),
            }, cancellationToken);

           
        }


        private async Task<DialogTurnResult> ShowHospitalStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if(((FoundChoice)stepContext.Result).Value.Equals("Oui"))
            {
                List<Hopitaux> lesHopitaux = new List<Hopitaux>();
                DonneesExcel lesDonnees = new DonneesExcel();
                lesHopitaux = lesDonnees.TrouverLocalisation(localisation);
                string message="";
                for(int i=0;i<lesHopitaux.Count;i++)
                {
                    message += lesHopitaux[i].Nom_Etab + " \n ";
                }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Voici les hôpitaux concernés"), cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{message}"),cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
                
            }
            return await stepContext.EndDialogAsync(null, cancellationToken);


        }

        private async Task<DialogTurnResult> FinalHospitalFindingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static Task<bool> VerificationLocalisation(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {

            var donneesExcel = new DonneesExcel();

            return Task.FromResult(promptContext.Recognized.Succeeded && donneesExcel.ExistLocalisationHopital(promptContext.Recognized.Value));

        }



        class DialogIds
        {
            public const String localisationPrompt="choixLocalisation"; 
            public const String validateLocalisationPrompt="valideLocalisation";
        }
    }
   
}
