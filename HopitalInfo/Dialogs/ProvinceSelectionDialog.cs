using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HopitalInfo.Dialogs
{
    public class ProvinceSelectionDialog : CancelAndInfoDialog
    {
        private BotState _userState;
        private static List<Hopitaux> lesHopitaux;
        public ProvinceSelectionDialog(InfoCategorieDialog infoCategorieDialog,HospitalInfoRecognizer luisRecognizer,UserState userState)
            : base(luisRecognizer, nameof(ProvinceSelectionDialog),infoCategorieDialog)
        {
            InitialDialogId = nameof(ProvinceSelectionDialog);
            _userState = userState;
            AddDialog(new TextPrompt(DialogIds.EnterProvincePrompt, VerificationProvince));
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
                Prompt = MessageFactory.Text("Veuillez entrer une province ou préfecture se trouvant dans la liste pré affichée."),
                RetryPrompt = MessageFactory.Text("Je suis désolé, mais le nom de la province ou préfecture entré n'est pas enregistré dans notre base de donnée. Veuillez s'il vous plaît entrer un nom valide."),
            };

            return await stepContext.PromptAsync(DialogIds.EnterProvincePrompt, promptOptions, cancellationToken);
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

        private static Task<bool> VerificationProvince(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(promptContext.Recognized.Succeeded && ExistProvince(promptContext.Recognized.Value));
        }

        private void SelectHospital(string province)
        {
            Predicate<Hopitaux> critere = (Hopitaux unHopital) => { return unHopital.Province.Trim().ToLowerInvariant() != province.Trim().ToLowerInvariant(); };
            lesHopitaux.RemoveAll(critere);
        }
        private static bool ExistProvince(string province)
        {
            Predicate<Hopitaux> critere = (Hopitaux unHopital) => { return unHopital.Province.Trim().ToLowerInvariant() == province.Trim().ToLowerInvariant(); };
            return lesHopitaux.Exists(critere);
        }
        class DialogIds
        {
            public const string EnterProvincePrompt = "enterProvincePrompt";
        }
    }
}
