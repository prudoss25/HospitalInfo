using CsvHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace HopitalInfo
{
    public class DonneesExcel
    {
        private Hopitaux[] DonneesHopitaux;
        private Hopitaux[] DonneesCentresSante;

        public DonneesExcel()
        {
            
            using (var readerHop = new StreamReader("Ressources\\Hopitaux.csv"))
            using (var hopitauxCsv = new CsvReader(readerHop))
            {
                hopitauxCsv.Configuration.HasHeaderRecord = false;
                DonneesHopitaux = hopitauxCsv.GetRecords<Hopitaux>().ToArray<Hopitaux>();
            }

            using (var readerCs = new StreamReader("Ressources\\CentresSante.csv"))
            using (var centresSanteCsv = new CsvReader(readerCs))
            {
                centresSanteCsv.Configuration.HasHeaderRecord = false;
                DonneesCentresSante = centresSanteCsv.GetRecords<Hopitaux>().ToArray<Hopitaux>();
            }
        }
        
        public bool ExistLocalisationHopital(string localisation)
        {
            for(int i=0;i<DonneesHopitaux.Length;i++)
            {
                if (DonneesHopitaux[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesHopitaux[i].Commune.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesHopitaux[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()))
                    return true;
            }
            for (int i = 0; i < DonneesCentresSante.Length; i++)
            {
                if (DonneesCentresSante[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesCentresSante[i].Commune.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesCentresSante[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()))
                    return true;
            }
            return false;
        }

        public bool ExistHopital(string hopital)
        {
            for (int i = 0; i < DonneesHopitaux.Length; i++)
            {
                if (DonneesHopitaux[i].Nom_Etab.Trim().ToLowerInvariant().Equals(hopital.Trim().ToLowerInvariant()))
                    return true;
            }
            for (int i = 0; i < DonneesCentresSante.Length; i++)
            {
                if (DonneesCentresSante[i].Nom_Etab.Trim().ToLowerInvariant().Equals(hopital.Trim().ToLowerInvariant()) )
                    return true;
            }
            return false;
        }
        public bool ExistCategorie(string categorie)
        {
            for (int i = 0; i < DonneesHopitaux.Length; i++)
            {
                if (DonneesHopitaux[i].Categorie.Trim().ToLowerInvariant().Equals(categorie.Trim().ToLowerInvariant()))
                    return true;
            }
            for (int i = 0; i < DonneesCentresSante.Length; i++)
            {
                if (DonneesCentresSante[i].Categorie.Trim().ToLowerInvariant().Equals(categorie.Trim().ToLowerInvariant()))
                    return true;
            }
            return false;
        }

        public List<Hopitaux> TrouverHopital(string hopital)
        {
            List<Hopitaux> lesHopitaux = new List<Hopitaux>();
            for (int i = 0; i < DonneesHopitaux.Length; i++)
            {
                if (DonneesHopitaux[i].Nom_Etab.Trim().ToLowerInvariant().Equals(hopital.Trim().ToLowerInvariant()))
                    lesHopitaux.Add(DonneesHopitaux[i]);
            }
            for (int i = 0; i < DonneesCentresSante.Length; i++)
            {
                if (DonneesCentresSante[i].Nom_Etab.Trim().ToLowerInvariant().Equals(hopital.Trim().ToLowerInvariant()))
                    lesHopitaux.Add(DonneesCentresSante[i]);
            }
            return lesHopitaux;
        }

        public List<Hopitaux> TrouverLocalisation(string localisation)
        {
            List<Hopitaux> lesHopitaux=new List<Hopitaux>();
            for (int i = 0; i < DonneesHopitaux.Length; i++)
            {
                if (DonneesHopitaux[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesHopitaux[i].Commune.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesHopitaux[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()))
                    lesHopitaux.Add(DonneesHopitaux[i]);
            }
            for (int i = 0; i < DonneesCentresSante.Length; i++)
            {
                if (DonneesCentresSante[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesCentresSante[i].Commune.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()) || DonneesCentresSante[i].Province.Trim().ToLowerInvariant().Equals(localisation.Trim().ToLowerInvariant()))
                    lesHopitaux.Add(DonneesCentresSante[i]);
            }

            return lesHopitaux;
        }
    }
}
