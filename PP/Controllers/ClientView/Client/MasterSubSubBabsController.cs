﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PP.Models;
using PP.Models.Master;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity;
using PP.ViewModels;
using Microsoft.AspNet.Identity.Owin;

namespace PP.Controllers.ClientView.Client
{
    public class MasterSubSubBabsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: MasterSubSubBabs
        public ActionResult Index()
        {
            return View();
        }

        public static double perhitunganHari(DateTime startD, DateTime endD)
        {
            double perhitunganAngka = 1 + ((endD - startD).TotalDays * 5 - (startD.DayOfWeek - endD.DayOfWeek) * 2) / 7;

            if (endD.DayOfWeek == DayOfWeek.Saturday) perhitunganAngka--;
            if (startD.DayOfWeek == DayOfWeek.Sunday) perhitunganAngka--;

            int keAngka = Convert.ToInt16(perhitunganAngka);
            if (keAngka <= 0)
            {
                perhitunganAngka = 1;
            }
            perhitunganAngka = keAngka;
            return perhitunganAngka;
        }

        public JsonResult Get()
        {
            var result = db.MasterSubSubBab.ToList();

            return Json(result, JsonRequestBehavior.AllowGet);

        }
        
        public ActionResult UploadBukus(long? id)
        {
            ViewBag.Data = (from subbab in db.MasterSubSubBab.Where(x => x.Id == id)
                            select subbab
                            ).ToList();
            return View();
        }

        [HttpPost]
        public ActionResult UploadBuku(long? Id, HttpPostedFileBase Dokuments)
        {
            if (Dokuments == null)
            {
                ViewBag.Error = "Please Select a Documents File";
                return View("Index");
            }
            else
            {
                string path = Server.MapPath("~/Dataupload/" + Dokuments.FileName);
                Dokuments.SaveAs(path);
                var dokuments = db.MasterSubSubBab.Single(m => m.Id == Id);
                dokuments.Id = Convert.ToInt64(Id);
                dokuments.Dokuments = Dokuments.FileName;
                db.Entry(dokuments).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
            }
            return RedirectToAction("Index", "MasterSubSubBabs");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult Ambil_Index()
        {
            var userStore = new UserStore<ApplicationUser>(db);
            var userManager = new UserManager<ApplicationUser>(userStore);
            var userId = User.Identity.GetUserId();
            var user = userManager.FindById(userId);

            var userManagerRole = Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var userRoles = userManagerRole.GetRoles(User.Identity.GetUserId());
            var role = userRoles[0];

            var kelompok = user.Kelompok;
            
            if (kelompok == "PPM" || kelompok == "DGM" || kelompok == "GM")
            {
                var mastersubbab = db.MasterSubSubBab.Include(m => m.SubBab).ToList();
                return Json(mastersubbab, JsonRequestBehavior.AllowGet);
            }
            else
            {
                if (role == "Pimkel") {
                    var masterAktivitas = (from masteraktivitas in db.MasterAktivitas
                                           select new SubBabSubBabVM
                                           {
                                               Hari = masteraktivitas.Hari,
                                               Target = masteraktivitas.Nama,
                                               PercentTarget = masteraktivitas.Percent
                                           }).ToList();

                    var masterSubBabsSaveOtomatis = (from kelompoks in db.MasterKelompok.Where(x => x.Nama == kelompok)
                                                     join bab in db.MasterBab on kelompoks.Id equals bab.KelompokId
                                                     join subbab in db.MasterSubBab on bab.Id equals subbab.BabId
                                                     join subsubbab in db.MasterSubSubBab on subbab.Id equals subsubbab.SubBabId into sbab
                                                     from ssbab in sbab
                                                     select new MasterSubSubBabVM
                                                     {
                                                         Id = ssbab.Id,
                                                         TanggalJatuhTempo = ssbab.TanggalJatuhTempo,
                                                         Pencapaian = ssbab.Pencapaian
                                                     }).ToList();

                    int checker = 0;
                    int sebelum = 0;
                    int setelah = 0;

                    foreach (var item in masterSubBabsSaveOtomatis)
                    {
                        checker = DateTime.Now.CompareTo(item.TanggalJatuhTempo);
                        if (checker > 0)
                        {
                            foreach (var item2 in masterAktivitas)
                            {
                                double hasilPerhitungan = perhitunganHari(item.TanggalJatuhTempo, DateTime.Now);
                                double hasilPencapaian = hasilPerhitungan / 68 * 100;
                                double PercentTarget = double.Parse(item2.PercentTarget, System.Globalization.CultureInfo.InvariantCulture);
                                if (item2.Target == item.Pencapaian)
                                {
                                    if (hasilPencapaian > PercentTarget)
                                    {
                                        var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item.Id);
                                        masterSubBabsInsert.PercentPencapaian = item2.PercentTarget;
                                        db.SaveChanges();
                                        db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                                    }
                                    if (hasilPencapaian < PercentTarget)
                                    {
                                        var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item.Id);
                                        masterSubBabsInsert.PercentPencapaian = hasilPencapaian.ToString();
                                        db.SaveChanges();
                                        db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                                    }
                                }
                                setelah = Convert.ToInt16(item2.Hari);

                                if (hasilPerhitungan == setelah)
                                {
                                    var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item.Id);
                                    masterSubBabsInsert.Target = item2.Target;
                                    masterSubBabsInsert.PercentTarget = item2.PercentTarget;
                                    db.SaveChanges();
                                    db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                                }
                                else if (hasilPerhitungan < setelah && hasilPerhitungan > sebelum)
                                {
                                    var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item.Id);
                                    masterSubBabsInsert.Target = item2.Target;
                                    masterSubBabsInsert.PercentTarget = item2.PercentTarget;
                                    db.SaveChanges();
                                    db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                                }
                                sebelum = Convert.ToInt16(item2.Hari);
                            }
                        }
                    }
                    var masterSubBabChange = (from kelompoks in db.MasterKelompok.Where(x => x.Nama == kelompok)
                                                     join bab in db.MasterBab on kelompoks.Id equals bab.KelompokId
                                                     join subbab in db.MasterSubBab on bab.Id equals subbab.BabId
                                                     join subsubbab in db.MasterSubSubBab on subbab.Id equals subsubbab.SubBabId into sbab
                                                     from ssbab in sbab
                                                     select new MasterSubSubBabVM
                                                     {
                                                         Id = ssbab.Id,
                                                         TanggalJatuhTempo = ssbab.TanggalJatuhTempo,
                                                         PercentTarget = ssbab.PercentTarget,
                                                         NamaPencapaian = ssbab.PercentPencapaian
                                                     }).ToList();

                    foreach (var item6 in masterSubBabChange)
                    {
                        double PercentPencapaian = double.Parse(item6.NamaPencapaian, System.Globalization.CultureInfo.InvariantCulture);
                        double PercentTarget = double.Parse(item6.PercentTarget, System.Globalization.CultureInfo.InvariantCulture);
                        if (PercentTarget < PercentPencapaian) {
                            var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item6.Id);
                            masterSubBabsInsert.StatusProposal = "A HEAD";
                            db.SaveChanges();
                            db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                        } else if (PercentTarget == PercentPencapaian) {
                            var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item6.Id);
                            masterSubBabsInsert.StatusProposal = "ON SCHEDULE";
                            db.SaveChanges();
                            db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                        } else if (PercentTarget > PercentPencapaian) {
                            var masterSubBabsInsert = db.MasterSubSubBab.Single(m => m.Id == item6.Id);
                            masterSubBabsInsert.StatusProposal = "BEHIND";
                            db.SaveChanges();
                            db.Entry(masterSubBabsInsert).State = System.Data.Entity.EntityState.Modified;
                        }
                    }

                }
                
                var mastersubbab = (from kelompoks in db.MasterKelompok.Where(x => x.Nama == kelompok)
                                    join bab in db.MasterBab on kelompoks.Id equals bab.KelompokId
                                    join subbab in db.MasterSubBab on bab.Id equals subbab.BabId
                                    join subsubbab in db.MasterSubSubBab on subbab.Id equals subsubbab.SubBabId into klmpk
                                    from klmpr in klmpk
                                    select new MasterSubSubBabVM
                                    {
                                        Id = klmpr.Id,
                                        NamaKelompok = kelompoks.Nama,
                                        Nama = klmpr.Nama,
                                        NoInstruksi = klmpr.NoInstruksi,
                                        TanggalBerlaku = klmpr.TanggalBerlaku,
                                        TanggalJatuhTempo = klmpr.TanggalJatuhTempo,
                                        TimeLine = klmpr.TimeLine,
                                        StatusProposal = klmpr.StatusProposal,
                                        Baca = klmpr.Baca,
                                        Pencapaian = klmpr.Pencapaian,
                                        NamaPencapaian = klmpr.PercentPencapaian,
                                        Target = klmpr.Target,
                                        PercentTarget = klmpr.PercentTarget
                                    }).ToList();
                return Json(mastersubbab, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetByNamaId(long? id)
        {
            var userStore = new UserStore<ApplicationUser>(db);
            var userManager = new UserManager<ApplicationUser>(userStore);
            var userId = User.Identity.GetUserId();
            var user = userManager.FindById(userId);

            var kelompok = user.Kelompok;
            var result = (from kelompoks in db.MasterKelompok.Where(x => x.Nama == kelompok)
                          join bab in db.MasterBab on kelompoks.Id equals bab.KelompokId
                          join subbab in db.MasterSubBab on bab.Id equals subbab.BabId
                          join subsubbab in db.MasterSubSubBab on subbab.Id equals subsubbab.SubBabId into klmpk
                          from klmpr in klmpk
                          select new MasterSubSubBabVM
                          {
                              Id = klmpr.Id,
                              NamaKelompok = kelompoks.Nama,
                              Nama = klmpr.Nama,
                              NoInstruksi = klmpr.NoInstruksi,
                              TanggalBerlaku = klmpr.TanggalBerlaku,
                              TanggalJatuhTempo = klmpr.TanggalJatuhTempo,
                              TimeLine = klmpr.TimeLine,
                              StatusProposal = klmpr.StatusProposal,
                              Baca = klmpr.Baca,
                              Urutan = klmpr.Urutan
                          }).SingleOrDefault(x => x.Id == id);

            var update = db.MasterSubSubBab.Single(m => m.Id == id);
            update.Id = Convert.ToInt64(id);
            update.Baca = "1";
            db.Entry(update).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetById(long? id)
        {
            var userStore = new UserStore<ApplicationUser>(db);
            var userManager = new UserManager<ApplicationUser>(userStore);
            var userId = User.Identity.GetUserId();
            var user = userManager.FindById(userId);

            var kelompok = user.Kelompok;
            var result = (from kelompoks in db.MasterKelompok.Where(x => x.Nama == kelompok)
                          join bab in db.MasterBab on kelompoks.Id equals bab.KelompokId
                          join subbab in db.MasterSubBab on bab.Id equals subbab.BabId
                          join subsubbab in db.MasterSubSubBab on subbab.Id equals subsubbab.SubBabId into klmpk
                          from klmpr in klmpk
                          select new MasterSubSubBabVM
                          {
                              Id = klmpr.Id,
                              NamaKelompok = kelompoks.Nama,
                              Nama = klmpr.Nama,
                              NoInstruksi = klmpr.NoInstruksi,
                              TanggalBerlaku = klmpr.TanggalBerlaku,
                              TanggalJatuhTempo = klmpr.TanggalJatuhTempo,
                              TimeLine = klmpr.TimeLine,
                              StatusProposal = klmpr.StatusProposal,
                              Urutan = klmpr.Urutan,
                              Baca = klmpr.Baca
                          }).SingleOrDefault(x => x.Id == id);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Save(MasterSubSubBab masterBab)
        {
            Int64 IdMaster = Convert.ToInt64(masterBab.PercentTarget);
            var result = (from u in db.MasterAktivitas
                          where u.Id == IdMaster
                          select u).SingleOrDefault();

            DateTime tanggalJatuhTempo;

            if (masterBab.TanggalBerlaku != null)
            {
                tanggalJatuhTempo = Convert.ToDateTime(masterBab.TanggalBerlaku);
                masterBab.TanggalJatuhTempo = tanggalJatuhTempo.AddYears(1);
                masterBab.TimeLine = masterBab.TanggalJatuhTempo.AddDays(68);
            }

            DateTime? akak = masterBab.TanggalJatuhTempo;

            if (masterBab.Id == 0)
            {
                //Ini untuk insert baru subbab
                masterBab.PercentPencapaian = result.Percent;
                masterBab.Pencapaian = result.Nama;
                masterBab.PercentTarget = result.Percent;
                masterBab.Target = result.Nama;
                masterBab.Baca = "0";
                db.MasterSubSubBab.Add(masterBab);
                var balik = db.SaveChanges();
                return Json(balik, JsonRequestBehavior.AllowGet);
            }
            else
            {
                // Ini untuk update subbab
                var masterbabDB = db.MasterSubSubBab.Single(m => m.Id == masterBab.Id);
                masterbabDB.Urutan = masterBab.Urutan;
                masterbabDB.SubBabId = masterBab.SubBabId;
                masterbabDB.Id = masterBab.Id;
                masterbabDB.Baca = "0";
                masterbabDB.Pencapaian = result.Nama;
                masterbabDB.PercentPencapaian = result.Percent;
                masterbabDB.TimeLine = masterBab.TimeLine;
                masterbabDB.NoInstruksi = masterBab.NoInstruksi;
                masterbabDB.StatusProposal = masterBab.StatusProposal;
                masterbabDB.Nama = masterBab.Nama;
                masterbabDB.TanggalBerlaku = masterBab.TanggalBerlaku;
                masterbabDB.TanggalJatuhTempo = masterBab.TanggalJatuhTempo;
                var balik = db.SaveChanges();
                db.Entry(masterbabDB).State = System.Data.Entity.EntityState.Modified;
                return Json(balik, JsonRequestBehavior.AllowGet);
            }
        }


        public ActionResult Update()
        {
            var userStore = new UserStore<ApplicationUser>(db);
            var userManager = new UserManager<ApplicationUser>(userStore);
            var userId = User.Identity.GetUserId();
            var user = userManager.FindById(userId);

            int currentYear = System.DateTime.Today.Year;
            int balik = 0;
            var kelompok = user.Kelompok;
            var mastersubBabs = (from kelompoks in db.MasterKelompok.Where(x => x.Nama == kelompok)
                                 join bab in db.MasterBab on kelompoks.Id equals bab.KelompokId
                                 join subbab in db.MasterSubBab on bab.Id equals subbab.BabId into klmpk
                                 from klmpr in klmpk/*.DefaultIfEmpty()*/
                                 select new SubBabSubBabVM
                                 {
                                     Id = klmpr.Id,
                                     TanggalJatuhTempo = klmpr.TanggalJatuhTempo
                                 }).ToList();

            var masterAktivitas = (from masteraktivitas in db.MasterAktivitas
                                   select new MasterAktivitasVM
                                   {
                                       Id = masteraktivitas.Id,
                                       Hari = masteraktivitas.Hari
                                   }).ToList();
            return Json(balik, JsonRequestBehavior.AllowGet);
        }



        public ActionResult Delete(long? id)
        {
            MasterSubSubBab masterBab = db.MasterSubSubBab.Find(id);
            db.MasterSubSubBab.Remove(masterBab);
            var response = db.SaveChanges();
            return Json(response, JsonRequestBehavior.AllowGet);
        }
    }
}
