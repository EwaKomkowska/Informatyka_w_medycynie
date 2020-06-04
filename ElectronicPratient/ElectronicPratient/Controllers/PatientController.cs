using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Web.Mvc;
using ElectronicPratient.Models;
using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Newtonsoft.Json;
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
                myPatients = myPatients.FindAll(s => s.Name[0].Family.ToLower().Contains(searchString.ToLower()));
            }

            //STRONICOWANIE 
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return View(myPatients.ToPagedList(pageNumber, pageSize));
        }


        public ViewResult ShowInfo(string id, DateTime? after, DateTime? before, int? page)
        {
            //POKAZYWANIE WSZYSTKICH INFORMACJI O PACJENCIE
            //ŁĄCZENIE Z SERWEREM
            var client = new FhirClient("http://localhost:8080/baseR4");        //second parameter - check standard version
            client.PreferredFormat = ResourceFormat.Json;
            Patient myPatient = client.Read<Patient>("Patient/" + id);

            UriBuilder UriBuilderx = new UriBuilder("http://localhost:8080/baseR4");
            UriBuilderx.Path = "Patient/" + myPatient.Id;
            Resource ReturnedResource = client.InstanceOperation(UriBuilderx.Uri, "everything");


            //DANE OSOBOWE PACJENTA
            ViewBag.Surname = myPatient.Name[0].Family;
            ViewBag.ID = myPatient.Id;
            ViewBag.Name = myPatient.Name[0].Given.FirstOrDefault();
            ViewBag.birthDate = new Date(myPatient.BirthDate.ToString());

            //WYSZUKIWANIE "EVERYTHING"
            var myElemList = new List<ShowInfo>();

            if (ReturnedResource is Bundle)
            {
                Bundle ReturnedBundle = ReturnedResource as Bundle;
                while (ReturnedBundle != null)
                {
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
                            myElem.date = Convert.ToDateTime(observation.Effective.ToString());
                            myElem.reason = observation.Code.Text;

                            myElemList.Add(myElem);
                        }
                        else if (Entry.Resource.TypeName == "MedicationRequest")
                        {
                            //LISTA MEDICATIONSTATEMENT
                            MedicationRequest mrequest = (MedicationRequest)Entry.Resource;

                            myElem.elemID = mrequest.Id;
                            myElem.originalModel = "MedicationRequest";

                            myElem.date = Convert.ToDateTime(mrequest.AuthoredOn.ToString());
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

            //OKREŚLENIE SZUKANEJ DATY
            if (before != null)
            {
                DateTime date = before.GetValueOrDefault();
                myElemList = myElemList.FindAll(s => s.date.Date.CompareTo(date.Date) < 0);
                ViewBag.Before = date;
            }

            if (after != null)
            {
                DateTime date = after.GetValueOrDefault();
                myElemList = myElemList.FindAll(s => s.date.Date.CompareTo(date.Date) > 0);
                ViewBag.After = date;
            }

            myElemList.Reverse();       //żeby daty były odpowiednio od najwyższej

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View(myElemList.ToPagedList(pageNumber, pageSize));
        }


        public ActionResult Chart(string id, DateTime? after, DateTime? before)
        {
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            Patient myPatient = client.Read<Patient>("Patient/" + id);        //5cbc121b-cd71-4428-b8b7-31e53eba8184

            UriBuilder UriBuilderx = new UriBuilder("http://localhost:8080/baseR4");
            UriBuilderx.Path = "Patient/" + myPatient.Id;
            Resource ReturnedResource = client.InstanceOperation(UriBuilderx.Uri, "everything");


            //DANE OSOBOWE PACJENTA
            ViewBag.Surname = myPatient.Name[0].Family;
            ViewBag.Name = myPatient.Name[0].Given.FirstOrDefault();
            ViewBag.ID = id;

            //WYSZUKIWANIE "EVERYTHING"
            var list = new List<ChartModel> { };

            if (ReturnedResource is Bundle)
            {
                Bundle ReturnedBundle = ReturnedResource as Bundle;
                while (ReturnedBundle != null)
                {
                    foreach (var Entry in ReturnedBundle.Entry)
                    {
                        //DANE DO OSI CZASU
                        if (Entry.Resource.TypeName == "Observation")
                        {
                            //LISTA BADAŃ WAGI
                            Observation observation = (Observation)Entry.Resource;

                            if (observation.Code.Text.Contains("Body Weight"))
                            {
                                var val = observation.Value as Quantity;
                                var amount = double.Parse((val.Value).ToString());
                                var date = observation.Effective.ToString().Substring(0, 10);
                                var point = new ChartModel(amount, date);
                                list.Add(point);
                            }
                        }
                    }

                    ReturnedBundle = client.Continue(ReturnedBundle, PageDirection.Next);
                }
            }
            else
            {
                throw new Exception("Operation call must return a bundle resource");
            }

            //OKREŚLENIE SZUKANEJ DATY
            if (before != null)
            {
                DateTime date = before.GetValueOrDefault();
                list = list.FindAll(s => Convert.ToDateTime(s.label).Date.CompareTo(date.Date) < 0);
                ViewBag.Before = date;
            }

            if (after != null)
            {
                DateTime date = after.GetValueOrDefault();
                list = list.FindAll(s => Convert.ToDateTime(s.label).Date.CompareTo(date.Date) > 0);
                ViewBag.After = date;
            }

            ViewBag.DataPoints = JsonConvert.SerializeObject(list);
            return View();
        }


        [HttpGet]
        public ActionResult EditObservation(string id, string type, string patientID)
        {
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            ViewBag.ID = patientID;

            if (type == "Observation")
            {
                Observation observation = client.Read<Observation>("Observation/" + id);
                EditObservation myObservation = new EditObservation();
                myObservation.Reason = observation.Code.Text;
                //myObservation.Date = Convert.ToDateTime(observation.Effective.ToString()).ToShortDateString();        //nie chciało dawać daty w okienku ładnie
                myObservation.ID = observation.Id;
                myObservation.Status = observation.Status;
                return View(myObservation);
            }

            ViewBag.Message = "Some Error until Redirect";
            return View();
        }


        [HttpGet]
        public ActionResult EditMedicationRequest(string id, string type, string patientID)
        {
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            ViewBag.ID = patientID;

            if (type == "MedicationRequest")
            {
                MedicationRequest request = client.Read<MedicationRequest>("MedicationRequest/" + id);
                EditMedicationRequest mrequest = new EditMedicationRequest();
                mrequest.Reason = (request.Medication as CodeableConcept).Text;
                if (request.DosageInstruction.Count() > 0)
                {
                    mrequest.Instruction = request.DosageInstruction[0].Text;
                }
                else
                {
                    mrequest.Instruction = "";
                }
                mrequest.ID = request.Id;
                return View(mrequest);
            } 
            
            ViewBag.Message = "Some Error until Redirect";
            return View();
        }


        [HttpGet]
        public ActionResult EditPatient(string id, string type)
        {
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            ViewBag.ID = id;

            if (type == "Patient")
            {
                Patient patient = client.Read<Patient>("Patient/" + id);
                EditPatient myPatient = new EditPatient();
                myPatient.Name = patient.Name[0].Given.FirstOrDefault();
                myPatient.Surname = patient.Name[0].Family;
                myPatient.Address = patient.Address[0].Text;
                myPatient.ID = patient.Id;
                return View(myPatient);
            }

            ViewBag.Message = "Some Error until Redirect";
            return View();
        }


        [HttpPost]
        public ActionResult EditObservation([Bind] EditObservation observation, string patientID)        //TODO: czy tu nie trzeba exclude czasem?
        {
            bool status = false;
            string Message = "";
            if (ModelState.IsValid)
            {
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;

                Observation original = client.Read<Observation>("Observation/" + observation.ID);
                original.Code.Text = observation.Reason;
                original.Status = observation.Status;

                client.Update(original);
                Message = "Your item successfully UPDATE";
                status = true;
            }
            else
            {
                Message = "You haven't got right model";
            }

            ViewBag.ID = patientID;
            ViewBag.Status = status;
            ViewBag.Message = Message;
            return View(observation);
        }


        [HttpPost]
        public ActionResult EditMedicationRequest([Bind] EditMedicationRequest request, string patientID)
        {
            bool status = false;
            string Message = "";

            if(ModelState.IsValid)
            {
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;

                MedicationRequest original = client.Read<MedicationRequest>("MedicationRequest/" + request.ID);
                (original.Medication as CodeableConcept).Text = request.Reason;
                Dosage dosage = new Dosage();
                dosage.Text = request.Instruction;
                original.DosageInstruction.Add(dosage);

                client.Update(original);
                Message = "Your item successfully UPDATE";
                status = true;
            }
            else
            {
                Message = "You haven't got right model";
            }

            ViewBag.ID = patientID;
            ViewBag.Status = status;
            ViewBag.Message = Message;
            return View(request);
        }


        [HttpPost]
        public ActionResult EditPatient([Bind] EditPatient patient)
        {
            bool status = false;
            string Message = "";

            if (ModelState.IsValid)
            {
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;

                Patient original = client.Read<Patient>("Patient/" + patient.ID);
                HumanName name = new HumanName();
                name.WithGiven(patient.Name).AndFamily(patient.Surname);
                original.Name.Add(name);
                Address address = new Address();
                address.Text = patient.Address;
                original.Address.Add(address);

                client.Update(original);
                Message = "Your item successfully UPDATE";
                status = true;
            }
            else
            {
                Message = "You haven't got right model";
            }

            ViewBag.ID = patient.ID;
            ViewBag.Status = status;
            ViewBag.Message = Message;
            return View(patient);
        }
    }
}