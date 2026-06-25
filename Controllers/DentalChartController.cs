using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DentalCollegeManagementSystem_AAU.Data;
using DentalCollegeManagementSystem_AAU.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DentalCollegeManagementSystem_AAU.Controllers
{
    [Route("DentalChart")]
    public class DentalChartController : Controller
    {
        private readonly AppDbContext _context;

        // ── FDI map: توليد رقم FDI من ToothId ──────────────────
        private static readonly Dictionary<string, int> FdiMap =
            Enumerable.Range(0, 16).Select(i => ($"U{i}", new[] { 18, 17, 16, 15, 14, 13, 12, 11, 21, 22, 23, 24, 25, 26, 27, 28 }[i]))
            .Concat(Enumerable.Range(0, 16).Select(i => ($"L{i}", new[] { 48, 47, 46, 45, 44, 43, 42, 41, 31, 32, 33, 34, 35, 36, 37, 38 }[i])))
            .ToDictionary(x => x.Item1, x => x.Item2);

        public DentalChartController(AppDbContext context)
        {
            _context = context;
        }

        // ════════════════════════════════════════════════════════
        //  VIEW
        // ════════════════════════════════════════════════════════

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index(int patientId)
        {
            if (patientId <= 0)
                return BadRequest("patientId مطلوب وأكبر من صفر.");

            ViewBag.PatientId = patientId;
            return View("~/Views/DentalCharting/Index.cshtml");
        }

        // ════════════════════════════════════════════════════════
        //  API — LOAD
        // ════════════════════════════════════════════════════════

        // GET /DentalChart/api/{patientId}  →  آخر جلسة
        [HttpGet("api/{patientId:int}")]
        public async Task<IActionResult> GetLatestChart(int patientId)
        {
            var session = await _context.DentalChartSessions
                .Include(s => s.ToothData)
                .Where(s => s.PatientId == patientId)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (session == null)
                return NotFound(new { message = "لا يوجد chart محفوظ لهذا المريض." });

            return Ok(BuildResponseDto(session));
        }

        // GET /DentalChart/api/single/{id}
        [HttpGet("api/single/{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var session = await _context.DentalChartSessions
                .Include(s => s.ToothData)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();
            return Ok(BuildResponseDto(session));
        }

        // GET /DentalChart/api/history/{patientId}
        [HttpGet("api/history/{patientId:int}")]
        public async Task<IActionResult> GetHistory(int patientId)
        {
            var list = await _context.DentalChartSessions
                .Where(s => s.PatientId == patientId)
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new DentalChartHistoryDto
                {
                    Id = s.Id,
                    SessionNote = s.SessionNote,
                    ReportJson = s.ReportJson,
                    BpeJson = s.BpeJson,
                    UpdatedAt = s.UpdatedAt
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET /DentalChart/api/report/{id}  →  تقرير منظّم للجلسة
        [HttpGet("api/report/{id:int}")]
        public async Task<IActionResult> GetReport(int id)
        {
            var session = await _context.DentalChartSessions
                .Include(s => s.ToothData)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            var report = BuildStructuredReport(session);
            return Ok(report);
        }

        // ════════════════════════════════════════════════════════
        //  API — SAVE
        // ════════════════════════════════════════════════════════

        // POST /DentalChart/api/save
        [HttpPost("api/save")]
        public async Task<IActionResult> SaveChart([FromBody] SaveDentalChartDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            List<DentalToothData> incoming;
            try { incoming = ParseChartDataJson(dto.ChartDataJson, sessionId: 0); }
            catch { return BadRequest(new { message = "ChartDataJson غير صالح." }); }

            // ولّد التقرير
            var reportJson = dto.ReportJson
                          ?? GenerateReportJson(dto.ChartDataJson);

            var session = await _context.DentalChartSessions
                .Include(s => s.ToothData)
                .Where(s => s.PatientId == dto.PatientId)
                .OrderByDescending(s => s.UpdatedAt)
                .FirstOrDefaultAsync();

            if (session != null)
            {
                _context.DentalToothData.RemoveRange(session.ToothData);
                session.SessionNote = dto.SessionNote;
                session.ReportJson = reportJson;
                session.BpeJson = NormalizeBpeJson(dto.BpeJson ?? session.BpeJson);
                session.UpdatedAt = DateTime.UtcNow;

                foreach (var row in incoming) row.SessionId = session.Id;
                _context.DentalToothData.AddRange(incoming);
            }
            else
            {
                session = new DentalChartSession
                {
                    PatientId = dto.PatientId,
                    SessionNote = dto.SessionNote,
                    ReportJson = reportJson,
                    BpeJson = NormalizeBpeJson(dto.BpeJson),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.DentalChartSessions.Add(session);
                await _context.SaveChangesAsync();

                foreach (var row in incoming) row.SessionId = session.Id;
                _context.DentalToothData.AddRange(incoming);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم الحفظ بنجاح.", id = session.Id });
        }

        // POST /DentalChart/api/save-session  →  جلسة جديدة دائماً
        [HttpPost("api/save-session")]
        public async Task<IActionResult> SaveNewSession([FromBody] SaveDentalChartDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            List<DentalToothData> incoming;
            try { incoming = ParseChartDataJson(dto.ChartDataJson, sessionId: 0); }
            catch { return BadRequest(new { message = "ChartDataJson غير صالح." }); }

            var reportJson = dto.ReportJson
                          ?? GenerateReportJson(dto.ChartDataJson);

            var session = new DentalChartSession
            {
                PatientId = dto.PatientId,
                SessionNote = dto.SessionNote,
                ReportJson = reportJson,
                BpeJson = NormalizeBpeJson(dto.BpeJson),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.DentalChartSessions.Add(session);
            await _context.SaveChangesAsync();

            foreach (var row in incoming) row.SessionId = session.Id;
            _context.DentalToothData.AddRange(incoming);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حفظ الجلسة الجديدة.", id = session.Id });
        }

        // PUT /DentalChart/api/update/{id}
        [HttpPut("api/update/{id:int}")]
        public async Task<IActionResult> UpdateSession(int id, [FromBody] SaveDentalChartDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var session = await _context.DentalChartSessions
                .Include(s => s.ToothData)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound(new { message = "Session not found." });

            List<DentalToothData> incoming;
            try { incoming = ParseChartDataJson(dto.ChartDataJson, sessionId: id); }
            catch { return BadRequest(new { message = "ChartDataJson غير صالح." }); }

            var reportJson = dto.ReportJson
                          ?? GenerateReportJson(dto.ChartDataJson);

            _context.DentalToothData.RemoveRange(session.ToothData);
            session.SessionNote = dto.SessionNote;
            session.ReportJson = reportJson;
            session.BpeJson = NormalizeBpeJson(dto.BpeJson ?? session.BpeJson);
            session.UpdatedAt = DateTime.UtcNow;

            foreach (var row in incoming) row.SessionId = id;
            _context.DentalToothData.AddRange(incoming);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated.", id = session.Id });
        }

        // DELETE /DentalChart/api/{id}
        [HttpDelete("api/{id:int}")]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var session = await _context.DentalChartSessions
                .Include(s => s.ToothData)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            _context.DentalToothData.RemoveRange(session.ToothData);
            _context.DentalChartSessions.Remove(session);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم الحذف." });
        }

        // ════════════════════════════════════════════════════════
        //  REPORT GENERATION
        // ════════════════════════════════════════════════════════

        /// <summary>
        /// يولّد ReportJson من chartDataJson —
        /// النتيجة: JSON array من { toothId, fdi, area, group, toolId, note }
        /// </summary>
        private static string GenerateReportJson(string chartDataJson)
        {
            var entries = new List<object>();

            try
            {
                var doc = JsonDocument.Parse(chartDataJson);
                var groups = new[] { "disease", "previous", "inside", "others" };
                var areas = new[] { "buccal", "lingual", "palatal", "mesial", "distal", "occlusal", "root" };

                foreach (var toothProp in doc.RootElement.EnumerateObject())
                {
                    var toothId = toothProp.Name;
                    var toothEl = toothProp.Value;
                    FdiMap.TryGetValue(toothId, out int fdi);

                    // whole
                    if (toothEl.TryGetProperty("whole", out var wholeEl))
                        ExtractEntries(entries, toothId, fdi, "whole", wholeEl, groups);

                    // areas
                    if (toothEl.TryGetProperty("areas", out var areasEl))
                        foreach (var areaProp in areasEl.EnumerateObject())
                            ExtractEntries(entries, toothId, fdi, areaProp.Name, areaProp.Value, groups);
                }
            }
            catch { /* JSON فاسد → نرجع array فاضي */ }

            return JsonSerializer.Serialize(entries);
        }

        private static void ExtractEntries(
            List<object> entries, string toothId, int fdi,
            string area, JsonElement slotEl, string[] groups)
        {
            foreach (var group in groups)
            {
                if (!slotEl.TryGetProperty(group, out var gEl)) continue;

                var toolIds = gEl.ValueKind == JsonValueKind.Array
                    ? gEl.EnumerateArray()
                         .Where(x => x.ValueKind == JsonValueKind.String)
                         .Select(x => x.GetString()!)
                         .ToList()
                    : gEl.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(gEl.GetString())
                        ? new List<string> { gEl.GetString()! }
                        : new List<string>();

                foreach (var toolId in toolIds)
                    entries.Add(new { toothId, fdi, area, group, toolId, note = (string?)null });
            }

            // note
            if (slotEl.TryGetProperty("note", out var noteEl)
                && noteEl.ValueKind == JsonValueKind.String
                && !string.IsNullOrEmpty(noteEl.GetString()))
            {
                entries.Add(new { toothId, fdi, area, group = "note", toolId = "note", note = noteEl.GetString() });
            }
        }

        /// <summary>تقرير منظّم كـ C# objects (للـ API endpoint)</summary>
        private static SessionReportDto BuildStructuredReport(DentalChartSession session)
        {
            var report = new SessionReportDto
            {
                SessionId = session.Id,
                PatientId = session.PatientId,
                SessionNote = session.SessionNote,
                UpdatedAt = session.UpdatedAt,
                Bpe = ParseBpeData(session.BpeJson)
            };

            var groups = new[] { "disease", "previous", "inside", "others" };

            foreach (var row in session.ToothData.OrderBy(r => r.ToothId))
            {
                FdiMap.TryGetValue(row.ToothId, out int fdi);

                var toothReport = report.Teeth.FirstOrDefault(t => t.ToothId == row.ToothId)
                               ?? new ToothReportDto { ToothId = row.ToothId, FdiCode = fdi };

                if (!report.Teeth.Contains(toothReport))
                    report.Teeth.Add(toothReport);

                var areaReport = new AreaReportDto { AreaName = row.AreaName, Note = row.Note };

                void AddTools(string? csv, string group)
                {
                    if (string.IsNullOrEmpty(csv)) return;
                    foreach (var tid in csv.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        areaReport.Tools.Add(new ToolEntryDto { Group = group, ToolId = tid });
                }

                AddTools(row.DiseaseToolId, "disease");
                AddTools(row.PreviousToolId, "previous");
                AddTools(row.InsideToolId, "inside");
                AddTools(row.OthersToolId, "others");

                if (areaReport.Tools.Any() || !string.IsNullOrEmpty(areaReport.Note))
                    toothReport.Areas.Add(areaReport);
            }

            report.TotalItems = report.Teeth.Sum(t => t.Areas.Sum(a => a.Tools.Count));
            return report;
        }

        // ════════════════════════════════════════════════════════
        //  BPE HELPERS
        // ════════════════════════════════════════════════════════

        private static readonly HashSet<string> AnteriorBpeValues = new()
        {
            "0", "1", "2", "3", "4"
        };

        private static readonly HashSet<string> PosteriorBpeValues = new()
        {
            "0", "1", "2", "3", "4",
            "0*", "1*", "2*", "3*", "4*"
        };

        private static string NormalizeBpeJson(string? bpeJson)
        {
            var bpe = ParseBpeData(bpeJson);

            return JsonSerializer.Serialize(new
            {
                upperRight = bpe.UpperRight,
                upperAnterior = bpe.UpperAnterior,
                upperLeft = bpe.UpperLeft,
                lowerRight = bpe.LowerRight,
                lowerAnterior = bpe.LowerAnterior,
                lowerLeft = bpe.LowerLeft
            });
        }

        private static BpeDataDto ParseBpeData(string? bpeJson)
        {
            var result = new BpeDataDto();

            if (string.IsNullOrWhiteSpace(bpeJson))
                return result;

            try
            {
                using var doc = JsonDocument.Parse(bpeJson);
                var root = doc.RootElement;

                string Read(string camelName, string pascalName)
                {
                    JsonElement value;

                    if (!root.TryGetProperty(camelName, out value) &&
                        !root.TryGetProperty(pascalName, out value))
                    {
                        return "0";
                    }

                    return value.ValueKind == JsonValueKind.String
                        ? value.GetString() ?? "0"
                        : value.ToString();
                }

                result.UpperRight = NormalizePosteriorBpe(Read("upperRight", "UpperRight"));
                result.UpperAnterior = NormalizeAnteriorBpe(Read("upperAnterior", "UpperAnterior"));
                result.UpperLeft = NormalizePosteriorBpe(Read("upperLeft", "UpperLeft"));
                result.LowerRight = NormalizePosteriorBpe(Read("lowerRight", "LowerRight"));
                result.LowerAnterior = NormalizeAnteriorBpe(Read("lowerAnterior", "LowerAnterior"));
                result.LowerLeft = NormalizePosteriorBpe(Read("lowerLeft", "LowerLeft"));
            }
            catch
            {
                // Invalid/old BPE JSON: use six zero values.
            }

            return result;
        }

        private static string NormalizeAnteriorBpe(string? value)
        {
            var normalized = (value ?? "0").Trim();
            return AnteriorBpeValues.Contains(normalized) ? normalized : "0";
        }

        private static string NormalizePosteriorBpe(string? value)
        {
            var normalized = (value ?? "0").Trim();
            return PosteriorBpeValues.Contains(normalized) ? normalized : "0";
        }

        // ════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════

        private static List<DentalToothData> ParseChartDataJson(string json, int sessionId)
        {
            var rows = new List<DentalToothData>();
            var doc = JsonDocument.Parse(json);

            foreach (var toothProp in doc.RootElement.EnumerateObject())
            {
                var toothId = toothProp.Name;
                var toothEl = toothProp.Value;

                if (toothEl.TryGetProperty("whole", out var wholeEl))
                {
                    var slot = ReadSlot(wholeEl);
                    if (SlotHasData(slot))
                        rows.Add(MakeRow(sessionId, toothId, "whole", slot));
                }

                if (toothEl.TryGetProperty("areas", out var areasEl))
                    foreach (var areaProp in areasEl.EnumerateObject())
                    {
                        var slot = ReadSlot(areaProp.Value);
                        if (SlotHasData(slot))
                            rows.Add(MakeRow(sessionId, toothId, areaProp.Name, slot));
                    }
            }

            return rows;
        }

        private static string ReconstructChartDataJson(IEnumerable<DentalToothData> toothData)
        {
            var allTeeth = Enumerable.Range(0, 16).Select(i => $"U{i}")
                .Concat(Enumerable.Range(0, 16).Select(i => $"L{i}")).ToList();

            var allAreas = new[]
                { "buccal","lingual","palatal","mesial","distal","occlusal","root" };

            var grouped = toothData
                .GroupBy(d => d.ToothId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(d => d.AreaName));

            var chart = new Dictionary<string, object>();
            foreach (var toothId in allTeeth)
            {
                grouped.TryGetValue(toothId, out var areas);
                chart[toothId] = new
                {
                    id = toothId,
                    whole = SlotObject(areas?.GetValueOrDefault("whole")),
                    areas = allAreas.ToDictionary(a => a, a => (object)SlotObject(areas?.GetValueOrDefault(a)))
                };
            }

            return JsonSerializer.Serialize(chart);
        }

        private static DentalChartResponseDto BuildResponseDto(DentalChartSession s) => new()
        {
            Id = s.Id,
            PatientId = s.PatientId,
            ChartDataJson = ReconstructChartDataJson(s.ToothData),
            SessionNote = s.SessionNote,
            ReportJson = s.ReportJson,
            BpeJson = NormalizeBpeJson(s.BpeJson),
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };

        // ── Slot helpers ─────────────────────────────────────────
        private record SlotData(
            string? Disease, string? Previous,
            string? Inside, string? Others, string? Note);

        private static SlotData ReadSlot(JsonElement el)
        {
            static string? G(JsonElement e, string key)
            {
                if (!e.TryGetProperty(key, out var p)) return null;
                if (p.ValueKind == JsonValueKind.Array)
                {
                    var arr = p.EnumerateArray().ToList();
                    if (!arr.Any()) return null;
                    return string.Join(",", arr
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()));
                }
                if (p.ValueKind == JsonValueKind.String) return p.GetString();
                return null;
            }
            return new(G(el, "disease"), G(el, "previous"), G(el, "inside"), G(el, "others"), G(el, "note"));
        }

        private static bool SlotHasData(SlotData s) =>
            !string.IsNullOrEmpty(s.Disease) ||
            !string.IsNullOrEmpty(s.Previous) ||
            !string.IsNullOrEmpty(s.Inside) ||
            !string.IsNullOrEmpty(s.Others) ||
            !string.IsNullOrEmpty(s.Note);

        private static DentalToothData MakeRow(int sid, string tid, string area, SlotData s) => new()
        {
            SessionId = sid,
            ToothId = tid,
            AreaName = area,
            DiseaseToolId = s.Disease,
            PreviousToolId = s.Previous,
            InsideToolId = s.Inside,
            OthersToolId = s.Others,
            Note = s.Note
        };

        private static object SlotObject(DentalToothData? r) => new
        {
            disease = SplitToArray(r?.DiseaseToolId),
            previous = SplitToArray(r?.PreviousToolId),
            inside = SplitToArray(r?.InsideToolId),
            others = SplitToArray(r?.OthersToolId),
            note = r?.Note ?? ""
        };

        private static string[] SplitToArray(string? val) =>
            string.IsNullOrEmpty(val)
                ? Array.Empty<string>()
                : val.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }
}