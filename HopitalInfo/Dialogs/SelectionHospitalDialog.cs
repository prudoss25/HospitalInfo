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
    public class SelectionHospitalDialog:CancelAndInfoDialog
    {
        private BotState _userState;
        private string hospitalName;
        List<Hopitaux> lesHopitaux;
        DonneesExcel lesDonnees;
        public SelectionHospitalDialog(HospitalInfoRecognizer luisRecognizer,UserState userState, ProvinceSelectionDialog provinceSelectionDialog, CommuneSelectionDialog communeSelectionDialog, CategorieSelectionDialog categorieSelectionDialog, InfoCategorieDialog infoCategorieDialog)
            : base(luisRecognizer,nameof(SelectionHospitalDialog),infoCategorieDialog)
        {
            InitialDialogId = nameof(SelectionHospitalDialog);
            _userState = userState;
            lesDonnees = new DonneesExcel();
            lesHopitaux = new List<Hopitaux>();
            AddDialog(provinceSelectionDialog);
            AddDialog(communeSelectionDialog);
            AddDialog(categorieSelectionDialog);
            AddDialog(new ChoicePrompt(DialogIds.CaracteristicOptionPrompt));
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                AnswerRequestAnalysisStep,
                SelectHospitalByCaracteristicsStep,
                ShowSelectionStep,
                EndSelectionStep,
            }));
        }
        private async Task<DialogTurnResult> AnswerRequestAnalysisStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var hospitalNameStateAccessors = _userState.CreateProperty<string>(nameof(String));
            hospitalName = await hospitalNameStateAccessors.GetAsync(stepContext.Context, () => new String(""));
            var hospitalStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            lesHopitaux = await hospitalStateAccessors.GetAsync(stepContext.Context, () => new List<Hopitaux>());
            
            
            return await stepContext.PromptAsync(DialogIds.CaracteristicOptionPrompt,
            new PromptOptions
            {
                Prompt = MessageFactory.Text("Veillez Sélectionner un critère spécifique de recherche."),
                Choices = ChoiceFactory.ToChoices(new List<string> { "Préfecture/Province", "Ville/Commune", "Catégorie" }),
            }, cancellationToken);
        }

        private async Task<DialogTurnResult> SelectHospitalByCaracteristicsStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (FoundChoice)stepContext.Result;
            var userStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            await userStateAccessors.SetAsync(stepContext.Context, lesHopitaux, cancellationToken);
            switch (result.Value)
            {
                case "Préfecture/Province":
                    //Dialogue dirigeant le processus de sélection selon la Province/Préfecture
                    return await stepContext.BeginDialogAsync(nameof(ProvinceSelectionDialog), null, cancellationToken);
                case "Ville/Commune":
                    //Dialogue dirigeant le processus de sélection selon la Commune/Ville
                    return await stepContext.BeginDialogAsync(nameof(CommuneSelectionDialog), null, cancellationToken);
                case "Catégorie":
                    //Dialogue dirigeant le processus de sélection selon la Catégorie
                    return await stepContext.BeginDialogAsync(nameof(CategorieSelectionDialog), null, cancellationToken);
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Veuillez entrer un choix valide s'il vous plaît :"));
                    return await AnswerRequestAnalysisStep(stepContext, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> ShowSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var hospitalStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            lesHopitaux = await hospitalStateAccessors.GetAsync(stepContext.Context, () => new List<Hopitaux>());
            string message = "";
            for (int i = 0; i < lesHopitaux.Count; i++)
            {
                message += lesHopitaux[i].Nom_Etab + " situé dans la province " + lesHopitaux[i].Province + " plus précisement dans la commune " + lesHopitaux[i].Commune + " et de Catégorie " + lesHopitaux[i].Categorie + ". \n ";
            }
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Resultat trouvé :"), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{message}"), cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        class DialogIds
        {
            public const string CaracteristicOptionPrompt = "caracteristicOptionPrompt";
        }


    }
}
