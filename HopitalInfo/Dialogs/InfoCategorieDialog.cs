using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HopitalInfo.Dialogs
{
    public class InfoCategorieDialog:ComponentDialog
    {
        private BotState _userState;
        private static List<Hopitaux> lesHopitaux;
        private readonly HospitalInfoRecognizer _luisRecognizer;
        private string categorie;
        public InfoCategorieDialog(HospitalInfoRecognizer luisRecognizer,UserState userState)
            : base(nameof(InfoCategorieDialog))
        {
            InitialDialogId = nameof(InfoCategorieDialog);
            _userState = userState;
            _luisRecognizer = luisRecognizer;

            AddDialog(new TextPrompt(DialogIds.notifyCategoriePrompt, VerificationCategorie));
            AddDialog(new WaterfallDialog(InitialDialogId, new WaterfallStep[]
            {
                IntroInfoCategorieStep,
                ShowInfoCategorieStep,
                FinalInfoCategorieStep,
            }));
        }

        private async Task<DialogTurnResult> IntroInfoCategorieStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DonneesExcel donneesExcel = new DonneesExcel();
            if (stepContext.Options != null)
            {
                //Récupération de la catégorie identifiée
                categorie = "";
                var CategorieDetected = (String[])stepContext.Options;
                for (int i = 0; i < CategorieDetected.Length; i++)
                    categorie += CategorieDetected[i];
            }

            if (String.IsNullOrEmpty(categorie))
            {
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Veuillez entrer l'acronyme de la catégorie sur laquelle vous voulez vous informer."),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais cette catégorie n'est pas enregistrée dans notre base de donnée. Veuillez s'il vous plaît entrer une catégorie valide."),
                };

                return await stepContext.PromptAsync(DialogIds.notifyCategoriePrompt, promptOptions, cancellationToken);
            }
            else if (!donneesExcel.ExistCategorie(categorie))
            {
                // Asking Localisation.
                var promptOptions = new PromptOptions
                {
                    Prompt = MessageFactory.Text($"La catégorie {categorie} n'existe pas dans notre base de donnée. Veuillez entrer une catégorie valide"),
                    RetryPrompt = MessageFactory.Text("Je suis désolé, mais cette catégorie n'est pas enregistrée dans notre base de donnée. Veuillez s'il vous plaît entrer une catégorie valide."),
                };
                return await stepContext.PromptAsync(DialogIds.notifyCategoriePrompt, promptOptions, cancellationToken);
            }

            return await stepContext.NextAsync(categorie, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowInfoCategorieStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Traitement selon le type de Catégorie
            categorie = (string)stepContext.Result;
            switch(categorie.ToLowerInvariant().Trim())
            {
                case "hgu":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HGU signifie \"Hopital Général Universitaire\""),cancellationToken);
                    break;
                case "hsu":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HSU signifie \"Hopital Spécialisé Universitaire\""), cancellationToken);
                    break;
                case "hgr":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HGR signifie \"Hopital Général Régional\""), cancellationToken);
                    break;
                case "hsr":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HSR signifie \"Hopital Spécialisé Régional\""), cancellationToken);
                    break;
                case "hgp":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HGP signifie \"Hopital Général Provincial/Préfectoral\""), cancellationToken);
                    break;
                case "hsp":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HSP signifie \"Hopital Spécialisé Provincial/Préfectoral\""), cancellationToken);
                    break;
                case "hl":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("HL signifie \"Hopital Local\""), cancellationToken);
                    break;
                case "csu":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("CSU signifie \"Centre de santé de santé urbain\""), cancellationToken);
                    break;
                case "csua":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("CSUA signifie \"Centre de santé urbain avec module d'accouchement\""), cancellationToken);
                    break;
                case "csc":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("CSC signifie \"Centre de santé de santé Communal\""), cancellationToken);
                    break;
                case "csca":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("CSCA signifie \"Centre de santé de santé Communal avec module d'accouchement\""), cancellationToken);
                    break;
                case "dr":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("DR signifie \"Dispensaire Rural\""), cancellationToken);
                    break;
                case "lehm":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("LEHM signifie \"Laboratoire épidémiologique et d'hygiène de milieu\""), cancellationToken);
                    break;
                case "cdtmr":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("CDTMR signifie \"Centre de diagnostic et de Traitement des maladies respiratoires\""), cancellationToken);
                    break;
                case "crsr":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("CRSR signifie \"Centre de référence en santé roproductive\""), cancellationToken);
                    break;
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("la catégorie identifiée n'est pas correcte"), cancellationToken);
                    break;
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);

        }
        private async Task<DialogTurnResult> FinalInfoCategorieStep(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);

        }
        private static Task<bool> VerificationCategorie(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(promptContext.Recognized.Succeeded && ExistCategorie(promptContext.Recognized.Value));
        }

        
        private static bool ExistCategorie(string categorie)
        {
            Predicate<Hopitaux> critere = (Hopitaux unHopital) => { return unHopital.Categorie.Trim().ToLowerInvariant() == categorie.Trim().ToLowerInvariant(); };
            return lesHopitaux.Exists(critere);
        }
        class DialogIds
        {
            public const string notifyCategoriePrompt = "notifyCategorie";
        }
    }
}
