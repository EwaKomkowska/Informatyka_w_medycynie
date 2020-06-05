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
            //POŁĄCZENIE Z KLIENTEM
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            ViewBag.ID = patientID;

            if (type == "Observation")
            {
                //POBRANIE DANYCH O OBSERWACJI 
                Observation observation = client.Read<Observation>("Observation/" + id);
                EditObservation myObservation = new EditObservation();
                
                //PRZEKAZANIE DO EDYCJI
                myObservation.Reason = observation.Code.Text;
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
            //POŁĄCZENIE Z KLIENTEM
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            ViewBag.ID = patientID;

            if (type == "MedicationRequest")
            {
                //WYSZUKANIE ZASOBU
                MedicationRequest request = client.Read<MedicationRequest>("MedicationRequest/" + id);
                EditMedicationRequest mrequest = new EditMedicationRequest();

                //PRZEKAZANIE DANYCH
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
            //PODŁĄCZENIE KLIENTA
            var client = new FhirClient("http://localhost:8080/baseR4");
            client.PreferredFormat = ResourceFormat.Json;
            ViewBag.ID = id;

            if (type == "Patient")
            {
                //WYSZUKANIE ZASOBU
                Patient patient = client.Read<Patient>("Patient/" + id);
                EditPatient myPatient = new EditPatient();

                //PRZEKAZANIE DANYCH
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

            //SPRAWDZENIE MODELU
            if (ModelState.IsValid)
            {
                //PODŁĄCZENIE DO KLIENTA
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;

                //PRZEKAZANIE DANYCH
                Observation original = client.Read<Observation>("Observation/" + observation.ID);
                original.Code.Text = observation.Reason;
                original.Status = observation.Status;


                //UPDATE
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

            //SPRAWDZENIE DANYCH
            if(ModelState.IsValid)
            {
                //PODŁĄCZENIE KLIENTA
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;

                //PRZEKAZANIE DANYCH
                MedicationRequest original = client.Read<MedicationRequest>("MedicationRequest/" + request.ID);
                (original.Medication as CodeableConcept).Text = request.Reason;
                Dosage dosage = new Dosage();
                dosage.Text = request.Instruction;
                original.DosageInstruction.Add(dosage);

                //UPDATE
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

            //SPRAWDZENIE MODELU
            if (ModelState.IsValid)
            {
                //PODŁĄCZENIE KLIENTA
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;

                //PRZEKAZANIE DANYCH
                Patient original = client.Read<Patient>("Patient/" + patient.ID);
                HumanName name = new HumanName();
                name.WithGiven(patient.Name).AndFamily(patient.Surname);
                original.Name.Add(name);
                Address address = new Address();
                address.Text = patient.Address;
                original.Address.Add(address);

                //UPDATE
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


        public ActionResult HistoryPatient(string id, string type)
        {
            bool status = false;
            string Message = "";
            List<HistoryPatient> fullVersion = new List<HistoryPatient>();

            //SPRAWDZENIE MODELU
            if (ModelState.IsValid)
            {
                //PODŁĄCZENIE KLIENTA
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;
                //WYSZUKANIE HISTORII
                Bundle history = client.History("Patient/" + id);

                while (history != null)
                {
                    foreach (var e in history.Entry)
                    {
                        if (e.Resource.TypeName == "Patient")
                        {
                            //POBRANIE POTRZBNYCH DANYCH
                            HistoryPatient p = new HistoryPatient();
                            p.LastUpdate = e.Resource.Meta.LastUpdated;
                            p.VersionId = int.Parse(e.Resource.VersionId);

                            Patient patient1 = (Patient)e.Resource;
                            p.FirstName += patient1.Name[0].Given.FirstOrDefault();
                            p.Surname += patient1.Name[0].Family;

                            foreach (var elem in patient1.Address)
                            {
                                p.Address += elem.Text + " ";
                            }
                            fullVersion.Add(p);
                        }
                    }
                    history = client.Continue(history, PageDirection.Next);
                }
                status = true;
                ViewBag.ID = id;
            }
            else
            {
                Message = "You haven't got right model";
            }

            for (int i = 0; i < fullVersion.Count - 1; i++)
            {
                if (fullVersion[i].FirstName != fullVersion[i+1].FirstName)
                {
                    fullVersion[i].color[0] = "green";
                }
                if (fullVersion[i].Surname != fullVersion[i+1].Surname)
                {
                    fullVersion[i].color[1] = "green";
                }
                if (fullVersion[i].Address != fullVersion[i+1].Address)
                {
                    fullVersion[i].color[2] = "green";
                }
            }
            
            ViewBag.Status = status;
            ViewBag.Message = Message;
            return View(fullVersion);
        }

        public ActionResult HistoryObservation(string id, string type, string patientID)
        {
            bool status = false;
            string Message = "";
            List<HistoryObservation> fullVersion = new List<HistoryObservation>();

            //SPRAWDZENIE MODELU
            if (ModelState.IsValid)
            {
                //PODŁĄCZENIE KLIENTA
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;
                //WYSZUKANIE HISTORII
                Bundle history = client.History("Observation/" + id);

                while (history != null)
                {
                    foreach (var e in history.Entry)
                    {
                        if (e.Resource.TypeName == "Observation")
                        {
                            //POBRANIE POTRZBNYCH DANYCH
                            HistoryObservation observation = new Models.HistoryObservation();
                            observation.LastUpdate = e.Resource.Meta.LastUpdated;
                            observation.VersionId = int.Parse(e.Resource.VersionId);

                            Observation obs = (Observation)e.Resource;
                            observation.Reason = obs.Code.Text;
                            observation.Status = obs.Status;

                            fullVersion.Add(observation);
                        }
                    }
                    history = client.Continue(history, PageDirection.Next);
                }
                status = true;
            }
            else
            {
                Message = "You haven't got right model";
            }

            for (int i = 0; i < fullVersion.Count - 1; i++)
            {
                if (fullVersion[i].Reason != fullVersion[i + 1].Reason)
                {
                    fullVersion[i].color[0] = "green";
                }
                if (fullVersion[i].Status != fullVersion[i + 1].Status)
                {
                    fullVersion[i].color[1] = "green";
                }
            }

            ViewBag.ID = patientID;
            ViewBag.Status = status;
            ViewBag.Message = Message;
            return View(fullVersion);
        }

        public ActionResult HistoryMedicationRequest(string id, string type, string patientID)
        {
            bool status = false;
            string Message = "";
            List<HistoryMedicationRequest> fullVersion = new List<HistoryMedicationRequest>();

            //SPRAWDZENIE MODELU
            if (ModelState.IsValid)
            {
                //PODŁĄCZENIE KLIENTA
                var client = new FhirClient("http://localhost:8080/baseR4");
                client.PreferredFormat = ResourceFormat.Json;
                //WYSZUKANIE HISTORII
                Bundle history = client.History("MedicationRequest/" + id);

                while (history != null)
                {
                    foreach (var e in history.Entry)
                    {
                        if (e.Resource.TypeName == "MedicationRequest")
                        {
                            //POBRANIE POTRZBNYCH DANYCH
                            HistoryMedicationRequest request = new HistoryMedicationRequest();
                            request.LastUpdate = e.Resource.Meta.LastUpdated;
                            request.VersionId = int.Parse(e.Resource.VersionId);

                            MedicationRequest medication = (MedicationRequest)e.Resource;
                            request.Reason += (medication.Medication as CodeableConcept).Text;
                            foreach (var elem in medication.DosageInstruction)
                            {
                                request.Instruction += elem.Text + " ";
                            }

                            fullVersion.Add(request);
                        }
                    }
                    history = client.Continue(history, PageDirection.Next);
                }
                status = true;
            }
            else
            {
                Message = "You haven't got right model";
            }

            for (int i = 0; i < fullVersion.Count - 1; i++)
            {
                if (fullVersion[i].Reason != fullVersion[i + 1].Reason)
                {
                    fullVersion[i].color[0] = "green";
                }
                if (fullVersion[i].Instruction != fullVersion[i + 1].Instruction)
                {
                    fullVersion[i].color[1] = "green";
                }
            }

            ViewBag.ID = patientID;
            ViewBag.Status = status;
            ViewBag.Message = Message;
            return View(fullVersion);
        }
    }
}