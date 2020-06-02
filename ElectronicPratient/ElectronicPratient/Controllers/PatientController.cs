using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ElectronicPratient.Models;
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

            //TODO: PRZEKAZAĆ ID PACJENTA
            Patient myPatient = client.Read<Patient>("Patient/5cbc121b-cd71-4428-b8b7-31e53eba8184");

            UriBuilder UriBuilderx = new UriBuilder("http://localhost:8080/baseR4");
            UriBuilderx.Path = "Patient/" + myPatient.Id;   
            Resource ReturnedResource = client.InstanceOperation(UriBuilderx.Uri, "everything");


            //DANE OSOBOWE PACJENTA
            ViewBag.Surname = myPatient.Name[0].Family;
            ViewBag.Name = myPatient.Name[0].Given.FirstOrDefault();
            ViewBag.birthDate = new Date(myPatient.BirthDate.ToString());


            //WYSZUKIWANIE "EVERYTHING"
            var myElemList = new List<ShowInfo>();

            if (ReturnedResource is Bundle)
            {
                Bundle ReturnedBundle = ReturnedResource as Bundle;
                while (ReturnedBundle != null)
                {
                    //Console.WriteLine("Received: " + ReturnedBundle.Total + " results, the resources are: ");
                    foreach (var Entry in ReturnedBundle.Entry)
                    {
                        ShowInfo myElem = new ShowInfo();
                        //DANE DO OSI CZASU
                        if (Entry.Resource.TypeName == "Observation")
                        {
                            //LISTA OBSERVATION
                            Observation observation = (Observation)Entry.Resource;

                            myElem.elemID = observation.Id;
                            myElem.originalModel = "Observation";

                            var amount = observation.Value as Quantity;
                            if (amount != null)
                                 myElem.amount = amount.Value + " " + amount.Unit;
                            myElem.date = new Date(observation.Effective.ToString().Substring(0, 10));
                            myElem.reason = observation.Code.Text;

                            myElemList.Add(myElem);
                        }
                        else if (Entry.Resource.TypeName == "MedicationRequest")
                        {
                            //LISTA MEDICATIONSTATEMENT
                            MedicationRequest mrequest = (MedicationRequest)Entry.Resource;

                            myElem.elemID = mrequest.Id;
                            myElem.originalModel = "MedicationRequest";

                            myElem.date = new Date(mrequest.AuthoredOn.ToString().Substring(0, 10));
                            myElem.reason += (mrequest.Medication as CodeableConcept).Text;
                            myElemList.Add(myElem);
                        }
                    }

                    ReturnedBundle = client.Continue(ReturnedBundle, PageDirection.Next);
                }
            }
            else
            {
                throw new Exception("Operation call must return a bundle resource");
            }

            //TODO!!!!
            //LISTA MEDICATION - tylko do uzupełinenia informacji

            return View(myElemList);
        }
    }
}