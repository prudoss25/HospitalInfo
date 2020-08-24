using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HopitalInfo.Dialogs
{
    public class CategorieSelectionDialog : CancelAndInfoDialog
    {
        private BotState _userState;
        private static List<Hopitaux> lesHopitaux;
        public CategorieSelectionDialog(InfoCategorieDialog infoCategorieDialog,HospitalInfoRecognizer luisRecognizer,UserState userState)
            : base(luisRecognizer,nameof(CategorieSelectionDialog),infoCategorieDialog)
        {
            InitialDialogId = nameof(CategorieSelectionDialog);
            _userState = userState;
            //AddDialog(infoCategorieDialog);
            AddDialog(new TextPrompt(DialogIds.EnterCategoriePrompt, VerificationCategorie));
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                IntroStep,
                HospitalSelectionStep,
                RecordSelectionStep,
                EndSelectionStep,
            }));

            
        }
        private async Task<DialogTurnResult> IntroStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            lesHopitaux = await userStateAccessors.GetAsync(stepContext.Context, () => new List<Hopitaux>());
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Veuillez entrer une catégorie d'hôpital se trouvant dans la liste pré affichée."),
                RetryPrompt = MessageFactory.Text("Je suis désolé, mais la catégorie entrée n'est pas enregistrée dans notre base de donnée. Veuillez s'il vous plaît entrer un nom valide."),
            };

            return await stepContext.PromptAsync(DialogIds.EnterCategoriePrompt, promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HospitalSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (string)stepContext.Result;
            var userStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            lesHopitaux = await userStateAccessors.GetAsync(stepContext.Context, () => new List<Hopitaux>());
           
            SelectHospital(result);
          
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> RecordSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userStateAccessors = _userState.CreateProperty<List<Hopitaux>>(nameof(List<Hopitaux>));
            await userStateAccessors.SetAsync(stepContext.Context, lesHopitaux, cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndSelectionStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static Task<bool> VerificationCategorie(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(promptContext.Recognized.Succeeded && ExistCategorie(promptContext.Recognized.Value));
        }

        private void SelectHospital(string categorie)
        {
            Predicate<Hopitaux> critere = (Hopitaux unHopital) => { return (!unHopital.Categorie.Trim().ToLowerInvariant().Equals(categorie.Trim().ToLowerInvariant())); };
            lesHopitaux.RemoveAll(critere);
        }
        private static bool ExistCategorie(string categorie)
        {
            Predicate<Hopitaux> critere = (Hopitaux unHopital) => { return unHopital.Categorie.Trim().ToLowerInvariant() == categorie.Trim().ToLowerInvariant(); };
            return lesHopitaux.Exists(critere);
        }
        class DialogIds
        {
            public const string EnterCategoriePrompt = "enterCategoriePrompt";
        }
    }
}
