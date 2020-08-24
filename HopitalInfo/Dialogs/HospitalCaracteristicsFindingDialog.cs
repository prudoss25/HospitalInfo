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
    public class HospitalCaracteristicsFindingDialog : CancelAndInfoDialog
    {
        private BotState _userState;
        private string hospitalName;
        List<Hopitaux> lesHopitaux;
        DonneesExcel lesDonnees;
        public HospitalCaracteristicsFindingDialog(InfoCategorieDialog infoCategorieDialog,HospitalInfoRecognizer luisRecognizer,UserState userState, SelectionHospitalDialog selectionHospitalDialog,ProvinceSelectionDialog provinceSelectionDialog, CommuneSelectionDialog communeSelectionDialog, CategorieSelectionDialog categorieSelectionDialog)
            : base(luisRecognizer,nameof(HospitalCaracteristicsFindingDialog),infoCategorieDialog)
        {
            InitialDialogId = nameof(HospitalCaracteristicsFindingDialog);
            _userState = userState;
            lesDonnees = new DonneesExcel();
            lesHopitaux = new List<Hopitaux>();

            AddDialog(provinceSelectionDialog);
            AddDialog(communeSelectionDialog);
            AddDialog(categorieSelectionDialog);
            AddDialog(selectionHospitalDialog);
            AddDialog(new ChoicePrompt(DialogIds.CaracteristicOptionPrompt));
            AddDialog(new ChoicePrompt(DialogIds.SelectionContinue));
            AddDialog(new TextPrompt(DialogIds.HospitalPrompt, VerificationHospital));
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                InstroHospitalCaracteristicsFindingStep,
                AnswerRequestAnalysisStep,
                SelectionProcessStep,
                SelectionProcessConfirmStep,
                RepeatProcessStep,
                ShowResultStep,
            }));
        }

        private async Task<DialogTurnResult> InstroHospitalCaracteristicsFindingStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DonneesExcel donneesExcel = new DonneesExcel();

            if(stepContext.Options!=null)
            {
                //Enregistrement du Nom d'Hôpital Détecté
                hospitalName = "";
                var hospitalNameDetected = (String[])stepContext.Options;
                for (int i = 0; i < hospitalNameDetected.Length; i++)
                    hospitalName += hospitalNameDetected[i];
            }
            
            if (String.IsNullOrEmpty(hospitalName))
            {
                // Asking Hospital Name
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Veuillez entrer le nom de l'hôpital."),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais le nom de l'hôpital entré n'est pas enregistré dans notre base de donnée. Veuillez s'il vous plaît entrer un nom valide."),
                };

                return await stepContext.PromptAsync(DialogIds.HospitalPrompt, promptOptions, cancellationToken);
            }
            else if (!donneesExcel.ExistHopital(hospitalName))
            {
                // Asking Hospital Name.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text($"L'Hôpital {hospitalName} n'existe pas dans notre base de donnée. Veuillez entrer un nom d'Hôpital valide"),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais cet hôpital ou centre de santé n'est pas enregistré dans notre base de donnée. Veuillez s'il vous plaît entrer une localisation valide."),
                };
                return await stepContext.PromptAsync(DialogIds.HospitalPrompt, promptOptions, cancellationToken);
            }

            return await stepContext.NextAsync(hospitalName, cancellationToken);
        }

        private async Task<DialogTurnResult> AnswerRequestAnalysisStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            hospitalName = (string)stepContext.Result;
            var hospitalNameStateAccessors = _userState.CreateProperty<string>(nameof(String));
            await hospitalNameStateAccessors.SetAsync(stepContext.Context, hospitalName, cancellationToken);
            lesHopitaux = lesDonnees.TrouverHopital(hospitalName);
            var listHopitauxStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            await listHopitauxStateAccessors.SetAsync(stepContext.Context, lesHopitaux , cancellationToken);


            if (lesHopitaux.Count==1)
            {
                //Afficher lla liste dans le cas où nous avons un seul Hôpital
                return await ShowResultStep(stepContext, cancellationToken);
            }
            else
            {
                //Affichage de la liste des Hôpitaux trouvés
                string message = "";
                for (int i = 0; i < lesHopitaux.Count; i++)
                {
                    message += lesHopitaux[i].Nom_Etab + " situé dans la province "+lesHopitaux[i].Province+" plus précisement dans la commune "+ lesHopitaux[i].Commune + " et de Catégorie "+ lesHopitaux[i].Categorie+". \n ";
                }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Plusieurs Hôpitaux à ce nom ont été trouvé :"), cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{message}"), cancellationToken);
                return await stepContext.BeginDialogAsync(nameof(SelectionHospitalDialog),null,cancellationToken);
            } 
        }

       

        private async Task<DialogTurnResult> SelectionProcessStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            lesHopitaux = await userStateAccessors.GetAsync(stepContext.Context, () => new List<Hopitaux>());
            if (lesHopitaux.Count == 1)
                return await EndDialogStep(stepContext, cancellationToken);
            else
            {
                return await stepContext.PromptAsync(DialogIds.SelectionContinue,
            new PromptOptions
            {
                //Damande de confirmation de la poursuite du processus de sélection
                Prompt = MessageFactory.Text("Voulez vous continuer le processus de sélection ."),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Oui", "Non" }),
            }, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> SelectionProcessConfirmStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (FoundChoice)stepContext.Result;
            switch(result.Value)
            {
                case "Oui":
                    //Lancement du Processus de sélection des résultats pertinents
                    return await stepContext.BeginDialogAsync(nameof(SelectionHospitalDialog), null, cancellationToken);
                case "Non":
                    //Fin du Dialog
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Merci de nous avoir consulté"));
                    return await EndDialogStep(stepContext, cancellationToken);
                default:
                    return await SelectionProcessStep(stepContext, cancellationToken);
            }
        }

        private async Task<DialogTurnResult>RepeatProcessStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await SelectionProcessStep(stepContext, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowResultStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var message = $"L'hopital {lesHopitaux[0].Nom_Etab} est un Hôpital de catégorie {lesHopitaux[0].Categorie} située dans la province {lesHopitaux[0].Province} plus précisement dans la commune {lesHopitaux[0].Commune}.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(message));
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndDialogStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
            public const string HospitalPrompt = "hospitalChoosePrompt";
            public const string CaracteristicOptionPrompt = "caracteristicOptionPrompt";
            public const string SelectionContinue = "selectionContinue";
        }
    }

    
}
