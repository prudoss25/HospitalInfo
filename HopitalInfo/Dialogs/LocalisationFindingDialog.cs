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
    public class LocalisationFindingDialog : CancelAndInfoDialog
    {
        private BotState _userState;
        private String hospitalName;
        public LocalisationFindingDialog(InfoCategorieDialog infoCategorieDialog,HospitalInfoRecognizer luisRecognizer,UserState userState)
            : base(luisRecognizer,nameof(LocalisationFindingDialog),infoCategorieDialog)
        {
            InitialDialogId = nameof(LocalisationFindingDialog);
            _userState = userState;
            AddDialog(new ChoicePrompt(DialogIds.localisationOptionPrompt));
            AddDialog(new TextPrompt(DialogIds.notifyHospitalPrompt,VerificationHospital));
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                IntroLocalisationFindingStep,
                ChoiceOptionLocalisationStep,
                ShowLocalisationStep,
                FinalLocalisatinFindingStep,
            }));
        }

        private async Task<DialogTurnResult> IntroLocalisationFindingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DonneesExcel donneesExcel = new DonneesExcel();
            if(stepContext.Options!=null)
            {
                hospitalName = "";
                var hospitalNameDetected = (String[])stepContext.Options;
                for (int i = 0; i < hospitalNameDetected.Length; i++)
                    hospitalName += hospitalNameDetected[i];
            }
            
            if(String.IsNullOrEmpty(hospitalName))
            {
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Veuillez entrer le nom de l'Hôpital ou du Centre de Santé recherché."),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais cet hôpital ou centre de santé n'est pas enregistré dans notre base de donnée. Veuillez s'il vous plaît entrer un nom valide."),
                };

                return await stepContext.PromptAsync(DialogIds.notifyHospitalPrompt, promptOptions, cancellationToken);
            }
            else if (!donneesExcel.ExistHopital(hospitalName))
            {
                // Asking Localisation.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text($"L'Hôpital {hospitalName} n'existe pas dans notre base de donnée. Veuillez entrer un nom d'Hôpital valide"),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais cet hôpital ou centre de santé n'est pas enregistré dans notre base de donnée. Veuillez s'il vous plaît entrer un nom valide."),
                };
                return await stepContext.PromptAsync(DialogIds.notifyHospitalPrompt, promptOptions, cancellationToken);
            }

            return await stepContext.NextAsync(hospitalName, cancellationToken);
        }

        private async Task<DialogTurnResult> ChoiceOptionLocalisationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            hospitalName = (string)stepContext.Result;
            return await stepContext.PromptAsync(DialogIds.localisationOptionPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Veillez Sélectionner le type de localisation requis."),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Préfecture/Province", "Ville/Commune","Tout" }),
                }, cancellationToken);

        }

        private async Task<DialogTurnResult> ShowLocalisationStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DonneesExcel lesDonnees = new DonneesExcel();
            List<Hopitaux> lesHopitaux;
            lesHopitaux = lesDonnees.TrouverHopital(hospitalName);

            var localisationType = ((FoundChoice)stepContext.Result).Value;
            var message = "";
            switch (localisationType)
            {
                case "Préfecture/Province":
                    message = "";
                    for (int i=0;i<lesHopitaux.Count;i++)
                    {
                        message += $"La province de l'hôpital {hospitalName} est : {lesHopitaux[i].Province}" +"\n";
                    }
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(message));
                    break;
                case "Ville/Commune":
                    message = "";
                    for (int i = 0; i < lesHopitaux.Count; i++)
                    {
                        message += $"La commune de l'hôpital {hospitalName} est : {lesHopitaux[i].Commune}" + "\n";
                    }
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(message));
                    break;
                case "Tout":
                    message = "";
                    for (int i = 0; i < lesHopitaux.Count; i++)
                    {
                        message += $"La commune de l'hôpital {hospitalName} est : {lesHopitaux[i].Commune} ; Sa province est : {lesHopitaux[i].Province}" + "\n\n";
                    }
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(message));
                    break;
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Veuillez entrer un choix valide s'il vous plaît :"));
                    return await ChoiceOptionLocalisationStep(stepContext, cancellationToken); 
            }

            return await stepContext.NextAsync(null, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalLocalisatinFindingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);

        }

        private static Task<bool> VerificationHospital(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var donneesExcel = new DonneesExcel();

            return Task.FromResult(promptContext.Recognized.Succeeded && donneesExcel.ExistHopital(promptContext.Recognized.Value));

        }

        class DialogIds
        {
            public const string notifyHospitalPrompt = "chooseHospital";
            public const string localisationOptionPrompt = "chooseLocalisationType";
        }
    }
}
