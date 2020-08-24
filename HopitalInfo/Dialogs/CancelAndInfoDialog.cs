using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalInfo.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace HopitalInfo.Dialogs
{
    public class CancelAndInfoDialog : ComponentDialog
    {
        private const string InfoMsgText = "Affichage des informations sur les Catégories";
        private const string CancelMsgText = "Annulation...";
        private readonly HospitalInfoRecognizer _luisRecognizer;

        public CancelAndInfoDialog(HospitalInfoRecognizer luisRecognizer,string id,InfoCategorieDialog infoCategorieDialog)
           : base(id)
        {
            _luisRecognizer = luisRecognizer;
            AddDialog(infoCategorieDialog);
        }
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            var result = await InterruptAsync(innerDc, cancellationToken);
            if (result != null)
            {
                return result;
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        private async Task<DialogTurnResult> InterruptAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
                var luisResult = await _luisRecognizer.RecognizeAsync<HospitalInfoService>(innerDc.Context, cancellationToken);
                //var text = innerDc.Context.Activity.Text.ToLowerInvariant();

                switch (luisResult.TopIntent().intent)
                {
                    case HospitalInfoService.Intent.InfoCategorie:
                        var categorie = luisResult.Entities.Categorie;
                        return await innerDc.BeginDialogAsync(nameof(InfoCategorieDialog), categorie, cancellationToken);
                        //return new DialogTurnResult(DialogTurnStatus.Waiting);

                    case HospitalInfoService.Intent.Cancel:
                        var cancelMessage = MessageFactory.Text(CancelMsgText, CancelMsgText, InputHints.IgnoringInput);
                        await innerDc.Context.SendActivityAsync(cancelMessage, cancellationToken);
                        return await innerDc.CancelAllDialogsAsync(cancellationToken);
                }
            }

            return null;
        }
    }
}
