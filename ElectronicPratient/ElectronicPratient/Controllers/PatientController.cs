using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using PagedList;


namespace ElectronicPratient.Controllers
{
    public class PatientController : Controller
    {
        public ViewResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            var myPatients = new List<Patient>();

            //ŁĄCZENIE Z SERWEREM
            var client = new FhirClient("http://localhost:8080/baseR4");        //second parameter - check standard version
            client.PreferredFormat = ResourceFormat.Json;

            //POBIERANIE PACJENTÓW
            Bundle result = client.Search<Patient>();
            while (result != null)
            {
                foreach (var e in result.Entry)
                {
                    if (e.Resource.TypeName == "Patient")
                    {
                        Patient p = (Patient)e.Resource;
                        // do something with the resource
                        myPatients.Add(p);
                    }
                }
                result = client.Continue(result, PageDirection.Next);
            }

            //WYSZUKIWANIE PO NAZWISKU
            ViewBag.CurrentSort = sortOrder;
            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewBag.CurrentFilter = searchString;
            if (!String.IsNullOrEmpty(searchString))
            {
                myPatients = myPatients.FindAll(s => s.Name[0].Family.Contains(searchString));
            }

            //SORTOWANIE
            //TODO: trzeba to jakoś inaczej sortować, bo tak nie działa
            /*switch (sortOrder)
            {
                case "name":
                    myPatients.Sort();
                    break;
                default:  
                    myPatients.Reverse();
                    break;
            }*/

            //STRONICOWANIE 
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(myPatients.ToPagedList(pageNumber, pageSize));
        }


        public ActionResult ShowInfo()
        {
            //POKAZYWANIE WSZYSTKICH INFORMACJI O PACJENCIE

            //ŁĄCZENIE Z SERWEREM
            var client = new FhirClient("http://localhost:8080/baseR4");        //second parameter - check standard version
            client.PreferredFormat = ResourceFormat.Json;


            //DANE OSOBOWE PACJENTA
            //var pat = client.Read<Patient>("Patient/id");

            //var hist = client.History("Patient/5cbc121b-cd71-4428-b8b7-31e53eba8184", new FhirDateTime("2016-11-29").ToDateTimeOffset());

            /*var patient = new Patient();
            patient.Active = true;
            patient.Name.Add(new HumanName().WithGiven("Christopher").WithGiven("C.H.").AndFamily("Parks"));

           // ViewBag.Patient = patient;
           // ViewBag.History = hist;*/

            var myPatients = new List<Patient>();

            Bundle result = client.Search<Patient>();
            while (result != null)
            {
                foreach (var e in result.Entry)
                {
                    if (e.Resource.TypeName == "Patient")
                    {
                        Patient p = (Patient)e.Resource;
                        // do something with the resource
                        myPatients.Add(p);
                    }
                }
                result = client.Continue(result, PageDirection.Next);
            }



            //WYSZUKIWANIE "EVERYTHING"
            var myResourceList = new List<Resource>();
            var myObservation = new List<Observation>();
            var myMedicationStatement = new List<MedicationRequest>();


            UriBuilder UriBuilderx = new UriBuilder("http://localhost:8080/baseR4");
            UriBuilderx.Path = "Patient/" + myPatients[5].Id;   
            
            //TODO: dlaczego bierze tylko 10pierwszych???
            Resource ReturnedResource = client.InstanceOperation(UriBuilderx.Uri, "everything");

            if (ReturnedResource is Bundle)
            {
                Bundle ReturnedBundle = ReturnedResource as Bundle;
                //Console.WriteLine("Received: " + ReturnedBundle.Total + " results, the resources are: ");
                foreach (var Entry in ReturnedBundle.Entry)
                {
                    //ViewBag.Type = string.Format("{0}/{1}", Entry.Resource.TypeName, Entry.Resource.Id);
                    //myResourceList.Add(Entry.Resource);
                    ViewBag.Type += Entry.Resource.TypeName;

                    //DANE DO OSI CZASU
                    if (Entry.Resource.TypeName == "Observation")
                    {
                        //LISTA OBSERVATION
                        var observation = client.Read<Observation>("Observation/" + Entry.Resource.Id);
                        ViewBag.Message = observation.Code.TextElement;
                        ViewBag.Date = observation.Effective;
                        myObservation.Add(observation);
                    }
                    else if (Entry.Resource.TypeName == "MedicationRequest")
                    {
                        //LISTA MEDICATIONSTATEMENT
                        var statement = client.Read<MedicationRequest>("MedicationRequest/" + Entry.Resource.Id);
                        ViewBag.Message = statement.Status;
                        ViewBag.Date = statement.Intent;
                        myMedicationStatement.Add(statement);
                    }
                }
            }
            else
            {
                throw new Exception("Operation call must return a bundle resource");
            }

            //TODO!!!!
            //LISTA MEDICATION - tylko do uzupełinenia informacji

            ViewBag.Observation = myObservation;
            ViewBag.Medication = myMedicationStatement;

            return View(myResourceList);
        }

        [HttpGet]
        public ActionResult Create()
        {
            //TOWRZENIE NOWEGO PACJENTA 
            return View();
        }

        [HttpPost]
        public ActionResult Create(Patient patient)
        {
            //TODO: tu ma być dodanie nowego pacjenta
            return View();
        }
    }
}