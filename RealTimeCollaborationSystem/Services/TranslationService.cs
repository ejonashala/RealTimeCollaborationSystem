using RealTimeCollaborationSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Data;

namespace RealTimeCollaborationSystem.Services
{
    public class TranslationService : ITranslationService
    {
        private const string DefaultLanguage = "sq";
        private const string LanguageCacheKey = "CurrentLanguage";
        private readonly IHttpContextAccessor _http;
        private readonly AppDbContext _db;

        public TranslationService(IHttpContextAccessor http, AppDbContext db)
        {
            _http = http;
            _db = db;
        }

        public string T(string key)
        {
            var lang = CurrentLanguage();

            return lang switch
            {
                "en" => En.ContainsKey(key) ? En[key] : key,
                "sq" => Sq.ContainsKey(key) ? Sq[key] : key,
                _ => Sq.ContainsKey(key) ? Sq[key] : key
            };
        }

        public string CurrentLanguage()
        {
            var httpContext = _http.HttpContext;
            if (httpContext == null)
            {
                return DefaultLanguage;
            }

            if (httpContext.Items.TryGetValue(LanguageCacheKey, out var cachedLanguage)
                && cachedLanguage is string cached
                && NormalizeLanguage(cached) is string normalizedCached)
            {
                return normalizedCached;
            }

            var sessionLanguage = NormalizeLanguage(httpContext.Session.GetString("Language"));
            if (sessionLanguage != null)
            {
                return CacheLanguage(httpContext, sessionLanguage);
            }

            var userLanguage = GetLoggedInUserLanguage(httpContext);
            if (userLanguage != null)
            {
                httpContext.Session.SetString("Language", userLanguage);
                return CacheLanguage(httpContext, userLanguage);
            }

            return CacheLanguage(httpContext, DefaultLanguage);
        }

        private string? GetLoggedInUserLanguage(HttpContext httpContext)
        {
            var userIdRaw = httpContext.Session.GetString("UserId");
            if (!int.TryParse(userIdRaw, out var userId))
            {
                return null;
            }

            var language = _db.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.Language)
                .FirstOrDefault();

            return NormalizeLanguage(language);
        }

        private static string CacheLanguage(HttpContext httpContext, string language)
        {
            httpContext.Items[LanguageCacheKey] = language;
            return language;
        }

        private static string? NormalizeLanguage(string? language)
        {
            if (string.Equals(language, "sq", StringComparison.OrdinalIgnoreCase))
            {
                return "sq";
            }

            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                return "en";
            }

            return null;
        }

        private static readonly Dictionary<string, string> Sq = new()
        {
            //Homepage
            ["Features"] = "Veçoritë",
            ["How It Works"] = "Si funksionon",
            ["Benefits"] = "Përfitimet",

            ["Login"] = "Kyçu",
            ["Register"] = "Regjistrohu",
            ["Welcome back"] = "Mirë se u ktheve",
            ["Name"] = "Emri",
            ["Enter your email"] = "Shkruaj email-in tënd",
            ["Enter your password"] = "Shkruaj fjalëkalimin tënd",
            ["Log In"] = "Kyçu",
            ["Don't have an account?"] = "Nuk ke llogari?",
            ["Create account"] = "Krijo llogari",
            ["Forgot your password?"] = "Ke harruar fjalëkalimin?",
            ["Reset it"] = "Rivendose",
            ["Create your account"] = "Krijo llogarinë tënde",
            ["Enter your name"] = "Shkruaj emrin tënd",
            ["Enter a password"] = "Shkruaj një fjalëkalim",
            ["Confirm Password"] = "Konfirmo fjalëkalimin",
            ["Repeat your password"] = "Përsërit fjalëkalimin",
            ["Create Account"] = "Krijo llogari",
            ["Already have an account?"] = "Ke tashmë llogari?",
            ["Log in"] = "Kyçu",
            ["Reset your password"] = "Rivendos fjalëkalimin",
            ["Continue"] = "Vazhdo",
            ["Back to Login"] = "Kthehu te kyçja",
            ["Set your new password"] = "Vendos fjalëkalimin e ri",
            ["Enter your new password"] = "Shkruaj fjalëkalimin e ri",
            ["Repeat your new password"] = "Përsërit fjalëkalimin e ri",

            ["Home"] = "Ballina",
            ["Privacy"] = "Privatësia",

            ["Real-time academic collaboration for students and professors, built for clarity, structure, and progress."] =
            "Bashkëpunim akademik në kohë reale për studentë dhe profesorë, i ndërtuar për qartësi, strukturë dhe progres.",

            ["Current focus"] = "Fokusi aktual",

            ["Core capabilities"] = "Aftësitë kryesore",

            ["Real-time academic collaboration platform"] = "Platformë akademike për bashkëpunim në kohë reale",
            ["CollabSpace brings students and professors into one structured, live academic workspace."] = "CollabSpace i bashkon studentët dhe profesorët në një hapësirë akademike të strukturuar dhe aktive.",
            ["Manage project topics, tasks, quizzes, teamwork, progress, and professor-student coordination in a platform designed for clarity and real-time momentum."] = "Menaxho temat e projekteve, detyrat, kuizet, punën ekipore, progresin dhe koordinimin profesor-student në një platformë të qartë dhe dinamike.",
            ["Explore Features"] = "Shiko veçoritë",
            ["See How It Works"] = "Shiko si funksionon",
            ["Role-aware coordination"] = "Koordinim sipas rolit",
            ["Designed for both students and professors"] = "E krijuar për studentë dhe profesorë",
            ["Live academic workflow"] = "Rrjedhë akademike në kohë reale",
            ["Tasks, progress, and updates stay connected"] = "Detyrat, progresi dhe përditësimet mbeten të lidhura",
            ["Built for students and professors"] = "E ndërtuar për studentë dhe profesorë",
            ["Organized academic workflow"] = "Rrjedhë akademike e organizuar",
            ["Live updates and shared progress"] = "Përditësime në kohë reale dhe progres i përbashkët",
            ["CollabSpace Workspace"] = "Hapësira CollabSpace",
            ["Workspace preview"] = "Pamje e hapësirës",
            ["Academic coordination, in one place"] = "Koordinimi akademik në një vend",
            ["Live sync"] = "Sinkronizim live",
            ["Realtime"] = "Kohë reale",
            ["Team activity"] = "Aktiviteti i ekipit",
            ["Structured"] = "E strukturuar",
            ["Task flow"] = "Rrjedha e detyrave",
            ["Visible"] = "E dukshme",
            ["Progress + feedback"] = "Progres + feedback",
            ["One organized view for collaboration, planning, and progress."] = "Një pamje e organizuar për bashkëpunim, planifikim dhe progres.",
            ["CollabSpace keeps topic decisions, tasks, guidance, quizzes, and updates in one connected academic environment."] = "CollabSpace mban vendimet për tema, detyrat, udhëzimet, kuizet dhe përditësimet në një mjedis akademik të lidhur.",
            ["Real-time teamwork spaces"] = "Hapësira ekipore në kohë reale",
            ["Shared work remains visible while groups collaborate and adjust quickly."] = "Puna e përbashkët mbetet e dukshme ndërsa grupet bashkëpunojnë dhe përshtaten shpejt.",
            ["Tasks, topics, and quizzes"] = "Detyra, tema dhe kuize",
            ["Academic work stays structured from early planning through evaluation."] = "Puna akademike mbetet e strukturuar nga planifikimi deri te vlerësimi.",
            ["Role-based guidance and visibility"] = "Udhëzim dhe dukshmëri sipas rolit",
            ["Students and professors get the clarity they need without added noise."] = "Studentët dhe profesorët marrin qartësinë që u duhet pa ngarkesë të panevojshme.",
            ["Project alignment"] = "Përputhja e projektit",
            ["Topic review, task assignment, and professor feedback remain synchronized."] = "Rishikimi i temave, caktimi i detyrave dhe feedback-u i profesorit mbeten të sinkronizuara.",
            ["Today"] = "Sot",
            ["Task updates published live"] = "Përditësimet e detyrave publikohen live",
            ["Quiz checkpoints ready"] = "Pikat e kontrollit të kuizeve janë gati",
            ["Professor guidance attached to work"] = "Udhëzimet e profesorit janë të lidhura me punën",
            ["A curated feature set built around the way academic collaboration actually works."] = "Një grup veçorish i ndërtuar sipas mënyrës si funksionon realisht bashkëpunimi akademik.",
            ["Instead of scattered tools and disconnected actions, CollabSpace organizes the most important parts of academic teamwork into a clear, focused experience."] = "Në vend të mjeteve të shpërndara, CollabSpace organizon pjesët më të rëndësishme të punës akademike në një përvojë të qartë.",
            ["Live collaboration"] = "Bashkëpunim live",
            ["Coordinate teamwork in real time without losing structure."] = "Koordino punën ekipore në kohë reale pa humbur strukturën.",
            ["Students and professors can follow activity as it happens while keeping discussions, tasks, and project movement inside an organized academic flow."] = "Studentët dhe profesorët mund të ndjekin aktivitetin ndërkohë që diskutimet, detyrat dhe progresi mbeten të organizuara.",
            ["Project planning"] = "Planifikimi i projektit",
            ["From topic selection to task management"] = "Nga zgjedhja e temës deri te menaxhimi i detyrave",
            ["Move from choosing project directions to assigning and managing work with better visibility and less confusion."] = "Kalo nga zgjedhja e drejtimit të projektit te caktimi dhe menaxhimi i punës me më shumë qartësi.",
            ["Evaluation and progress"] = "Vlerësimi dhe progresi",
            ["Track quizzes, milestones, and outcomes clearly"] = "Ndiq qartë kuizet, piketat dhe rezultatet",
            ["Keep academic progress easy to monitor with structured checkpoints, quizzes, and readable progress signals."] = "Mbaje progresin akademik të lehtë për t’u ndjekur me kuize, pika kontrolli dhe sinjale të qarta.",
            ["Role-aware workflow"] = "Rrjedhë pune sipas rolit",
            ["Built for students and professors together"] = "E ndërtuar për studentë dhe profesorë së bashku",
            ["Role-based access, clearer responsibilities, and professor-student interaction make the platform feel more intentional and easier to use."] = "Qasja sipas rolit, përgjegjësitë e qarta dhe ndërveprimi profesor-student e bëjnë platformën më të lehtë për përdorim.",
            ["Topic selection"] = "Zgjedhja e temës",
            ["Task ownership"] = "Përgjegjësia për detyrat",
            ["Teamwork spaces"] = "Hapësira ekipore",
            ["Professor interaction"] = "Ndërveprim me profesorin",
            ["Live notifications"] = "Njoftime live",
            ["Progress tracking"] = "Ndjekja e progresit",
            ["How it works"] = "Si funksionon",
            ["A clear path from account creation to live academic coordination."] = "Një rrugë e qartë nga krijimi i llogarisë deri te koordinimi akademik live.",
            ["The workflow stays simple and readable so users understand the platform quickly and move into meaningful work without friction."] = "Rrjedha mbetet e thjeshtë dhe e kuptueshme që përdoruesit ta kuptojnë shpejt platformën.",
            ["Create an account"] = "Krijo një llogari",
            ["Students and professors enter through a straightforward account setup experience."] = "Studentët dhe profesorët hyjnë përmes një procesi të thjeshtë të krijimit të llogarisë.",
            ["Enter with the right role"] = "Hyr me rolin e duhur",
            ["Each user lands in a role-aware environment shaped around academic responsibilities."] = "Çdo përdorues hyn në një mjedis të përshtatur sipas përgjegjësive akademike.",
            ["Manage or join academic work"] = "Menaxho ose bashkohu në punë akademike",
            ["Start projects, choose topics, form groups, and organize the work that needs to happen."] = "Nis projekte, zgjidh tema, krijo grupe dhe organizo punën që duhet bërë.",
            ["Collaborate in real time"] = "Bashkëpuno në kohë reale",
            ["Keep communication, tasks, decisions, and guidance moving inside one shared space."] = "Mbaj komunikimin, detyrat, vendimet dhe udhëzimet në një hapësirë të përbashkët.",
            ["Follow progress and evaluation"] = "Ndiq progresin dhe vlerësimin",
            ["Track quizzes, milestones, and updates so momentum stays visible and manageable."] = "Ndiq kuizet, piketat dhe përditësimet që progresi të mbetet i dukshëm.",
            ["Why CollabSpace"] = "Pse CollabSpace",
            ["A more organized academic environment leads to better coordination and fewer lost details."] = "Një mjedis akademik më i organizuar sjell koordinim më të mirë dhe më pak detaje të humbura.",
            ["CollabSpace is valuable because it makes academic collaboration easier to understand, easier to manage, and easier to keep moving."] = "CollabSpace është i vlefshëm sepse e bën bashkëpunimin akademik më të lehtë për t’u kuptuar dhe menaxhuar.",
            ["Centralized collaboration"] = "Bashkëpunim i centralizuar",
            ["Project work, communication, feedback, and progress stay connected instead of scattered across separate tools."] = "Puna e projektit, komunikimi, feedback-u dhe progresi mbeten të lidhura në vend që të shpërndahen në mjete të ndryshme.",
            ["Clearer structure"] = "Strukturë më e qartë",
            ["Students and professors can see responsibilities, progress, and next steps with much less ambiguity."] = "Studentët dhe profesorët mund të shohin përgjegjësitë, progresin dhe hapat e ardhshëm më qartë.",
            ["Platform value"] = "Vlera e platformës",
            ["Why teams work better with a shared academic system"] = "Pse ekipet punojnë më mirë me një sistem akademik të përbashkët",
            ["Better organization across the full workflow"] = "Organizim më i mirë gjatë gjithë rrjedhës së punës",
            ["From topic selection to task completion, the platform keeps academic work connected in one clear path."] = "Nga zgjedhja e temës deri te përfundimi i detyrës, platforma e mban punën akademike të lidhur.",
            ["Faster communication with less friction"] = "Komunikim më i shpejtë dhe më i lehtë",
            ["Professor-student interaction and live notifications make it easier to respond at the right time."] = "Ndërveprimi profesor-student dhe njoftimet live e bëjnë më të lehtë përgjigjen në kohën e duhur.",
            ["Clearer progress and more efficient management"] = "Progres më i qartë dhe menaxhim më efikas",
            ["Tasks, quizzes, milestones, and live updates help teams stay aligned without extra overhead."] = "Detyrat, kuizet, piketat dhe përditësimet live ndihmojnë ekipet të mbeten të koordinuara.",
            ["Get started"] = "Fillo tani",
            ["Bring structure, live coordination, and academic clarity into one premium platform."] = "Sill strukturë, koordinim live dhe qartësi akademike në një platformë të vetme.",
            ["Sign in to continue your work or create an account to start using CollabSpace with your students, professors, and teams."] = "Kyçu për të vazhduar punën ose krijo një llogari për të përdorur CollabSpace me studentët, profesorët dhe ekipet.",

            //Privacy
            ["Privacy Policy"] = "Politika e Privatësisë",
            ["Using this page to detail the site's privacy policy."] =
             "Përdor këtë faqe për të detajuar politikën e privatësisë së faqes.",

            //Sidebar Groups
            ["Groups"] = "Grupet",
            ["Group"] = "Grupi",
            ["Joined"] = "I bashkuar",
            ["Requested"] = "Kërkuar",
            ["Open"] = "Hapur",
            ["Active"] = "Aktiv",
            ["Draft"] = "Draft",
            ["Search"] = "Kërko",
            ["Clear"] = "Pastro",

            ["No groups available"] = "Nuk ka grupe të disponueshme",
            ["No groups found"] = "Nuk u gjetën grupe",
            ["Groups created by professors will appear here."] = "Grupet e krijuara nga profesorët do të shfaqen këtu.",
            ["Try another group name, professor, university, or field."] = "Provo një emër tjetër grupi, profesori, universiteti ose fushe.",

            ["capacity"] = "kapacitet",
            ["open spots"] = "vende të lira",
            ["filled"] = "të mbushura",
            ["created"] = "krijuar",
            ["students"] = "studentë",

            ["Email not provided"] = "Email-i nuk është dhënë",
            ["University not provided"] = "Universiteti nuk është dhënë",
            ["Field not provided"] = "Fusha nuk është dhënë",
            ["Selected"] = "Zgjedhur",

            ["View Topics"] = "Shiko temat",
            ["Your join request is pending."] = "Kërkesa jote për t’u bashkuar është në pritje.",
            ["Request to Join Group"] = "Kërko t’i bashkohesh grupit",

            ["Find groups"] = "Gjej grupe",
            ["Search by group, professor, university, field, or topic."] = "Kërko sipas grupit, profesorit, universitetit, fushës ose temës.",
            ["Search groups"] = "Kërko grupe",
            ["Group or professor..."] = "Grup ose profesor...",
            ["Newest first"] = "Më të rejat së pari",
            ["Ranked results"] = "Rezultate të renditura",
            ["All professor groups are sorted from newest to oldest."] = "Të gjitha grupet e profesorëve janë renditur nga më të rejat te më të vjetrat.",
            ["Matches are ranked by the closest group and professor details."] = "Përputhjet renditen sipas grupit dhe detajeve më të afërta të profesorit.",

            ["Topic groups"] = "Grupet e temave",
            ["Organized topic ownership"] = "Organizim i temave të zgjedhura",
            ["Use the Topics section to review available topic groups and your current selection."] = "Përdor seksionin Temat për të parë grupet e disponueshme dhe zgjedhjen aktuale.",

            ["No groups yet"] = "Nuk ka grupe ende",
            ["Create a group first, then student membership will appear here."] = "Krijo fillimisht një grup, pastaj anëtarësimi i studentëve do të shfaqet këtu.",
            ["No students in this group"] = "Nuk ka studentë në këtë grup",
            ["Search registered students and add them to this group."] = "Kërko studentë të regjistruar dhe shtoji në këtë grup.",
            ["Joined"] = "U bashkua",

            ["Students"] = "Studentët",
            ["Find registered students"] = "Gjej studentë të regjistruar",
            ["Search by name, email, university, or field of study, then add the student to a group."] = "Kërko sipas emrit, email-it, universitetit ose fushës së studimit, pastaj shtoje studentin në grup.",
            ["Search students"] = "Kërko studentë",
            ["Name, email, university..."] = "Emër, email, universitet...",
            ["Start with a search"] = "Fillo me kërkim",
            ["Student preview cards will appear here."] = "Kartat e studentëve do të shfaqen këtu.",
            ["No students found"] = "Nuk u gjetën studentë",
            ["Try another name, email, university, or field."] = "Provo një emër, email, universitet ose fushë tjetër.",
            ["Add to group"] = "Shto në grup",
            ["No groups with open spots"] = "Nuk ka grupe me vende të lira",
            ["Add Student"] = "Shto studentin",

            ["Requests"] = "Kërkesat",
            ["Join requests"] = "Kërkesat për bashkim",
            ["Pending requests for your groups."] = "Kërkesat në pritje për grupet e tua.",
            ["No pending requests"] = "Nuk ka kërkesa në pritje",
            ["Student requests to join your groups will appear here."] = "Kërkesat e studentëve për t’u bashkuar me grupet e tua do të shfaqen këtu.",
            ["Accept"] = "Prano",
            ["Deny"] = "Refuzo",

            //Index 
            ["Published groups"] = "Grupet e publikuara",
            ["Total topics"] = "Gjithsej tema",
            ["Pending submissions"] = "Dorëzime në pritje",
            ["Upcoming deadlines"] = "Afate të ardhshme",

            ["Selected topics"] = "Temat e zgjedhura",
            ["Assigned tasks"] = "Detyrat e caktuara",
            ["Pending tasks"] = "Detyra në pritje",

            ["Project Topics"] = "Temat e projektit",
            ["Selected Topic"] = "Tema e zgjedhur",
            ["Manage topics"] = "Menaxho temat",

            ["No topic selected"] = "Nuk ka temë të zgjedhur",
            ["Select a topic to show it here."] = "Zgjidh një temë që të shfaqet këtu.",

            ["Deadlines"] = "Afatet",
            ["Manage tasks"] = "Menaxho detyrat",

            ["No upcoming deadlines"] = "Nuk ka afate të ardhshme",
            ["Future task deadlines will appear here."] = "Afatet e ardhshme të detyrave do të shfaqen këtu.",

            ["Activity"] = "Aktiviteti",
            ["Recent Activity"] = "Aktiviteti i fundit",

            ["No recent activity"] = "Nuk ka aktivitet të fundit",

            ["New submissions, topic selections, and feedback updates will appear here."] =
            "Dorëzimet e reja, zgjedhjet e temave dhe përditësimet e feedback-ut do të shfaqen këtu.",

            //Projects
            ["Projects"] = "Projektet",

            ["Topic alignment"] = "Përputhja e temës",

            ["Shared project direction"] = "Drejtim i përbashkët i projektit",

            ["Keep titles, scope, deliverables, and team ownership clearly documented."] =
            "Mbaj titujt, qëllimin, rezultatet dhe përgjegjësitë e ekipit të dokumentuara qartë.",

            ["Progress"] = "Progresi",

            ["Milestones and deadlines"] = "Pikat kryesore dhe afatet",

            ["Track what is on schedule, what needs review, and what is ready to move forward."] =
            "Ndiq çfarë është sipas planit, çfarë ka nevojë për rishikim dhe çfarë është gati për të vazhduar.",

            //Settings
            ["Settings"] = "Cilësimet",

            ["Account"] = "Llogaria",

            ["Profile details"] = "Detajet e profilit",

            ["Keep personal information, language preferences, and workspace identity consistent."] =
            "Mbaj informacionin personal, preferencat e gjuhës dhe identitetin e hapësirës së punës të qëndrueshme.",

            ["Security"] = "Siguria",

            ["Access and protection"] = "Qasja dhe mbrojtja",

            ["Use account-related settings to support secure access alongside the current session-based sign-in flow."] =
            "Përdor cilësimet e llogarisë për të mbështetur qasje të sigurt së bashku me metodën aktuale të kyçjes me session.",

            //Tasks
            ["Tasks"] = "Tasks",

            ["Upcoming"] = "Upcoming",

            ["Deadlines that need focus"] = "Deadlines that need focus",

            ["Keep the next submissions visible so nothing important slips through the cracks."] =
            "Keep the next submissions visible so nothing important slips through the cracks.",

            ["Ownership"] = "Ownership",

            ["Clear responsibility"] = "Clear responsibility",

            ["Make it obvious who is doing what and where support is still needed."] =
            "Make it obvious who is doing what and where support is still needed.",

            //Tasks
            ["Tasks"] = "Detyrat",

            ["Upcoming"] = "Në vijim",

            ["Deadlines that need focus"] = "Afatet që kërkojnë vëmendje",

            ["Keep the next submissions visible so nothing important slips through the cracks."] =
            "Mbaji dorëzimet e ardhshme të dukshme që asgjë e rëndësishme të mos harrohet.",

            ["Ownership"] = "Përgjegjësia",

            ["Clear responsibility"] = "Përgjegjësi e qartë",

            ["Make it obvious who is doing what and where support is still needed."] =
             "Bëje të qartë kush po bën çfarë dhe ku nevojitet ende mbështetje.",

            //notifications
            ["Notifications"] = "Njoftimet",

            ["System updates"] = "Përditësimet e sistemit",

            ["Important updates about groups, tasks, deadlines, and feedback."] =
            "Përditësime të rëndësishme për grupet, detyrat, afatet dhe feedback-un.",

            ["Group invitation"] = "Ftesë për grup",

            ["Invite a student"] = "Fto një student",

            ["Student"] = "Studenti",

            ["Select student"] = "Zgjidh studentin",

            ["Topic group"] = "Grupi i temës",

            ["Select available topic"] = "Zgjidh temën e disponueshme",

            ["Send invitation"] = "Dërgo ftesë",

            ["Invitation status"] = "Statusi i ftesës",

            ["Sent invitations"] = "Ftesat e dërguara",

            ["No invitations sent"] = "Nuk ka ftesa të dërguara",

            ["Group invitations and their status will appear here."] =
            "Ftesat për grupe dhe statusi i tyre do të shfaqen këtu.",

            ["Pending"] = "Në pritje",

            ["Updates"] = "Përditësimet",

            ["Important notifications"] = "Njoftime të rëndësishme",

            ["No notifications"] = "Nuk ka njoftime",

            ["Important system updates will appear here."] =
            "Përditësimet e rëndësishme të sistemit do të shfaqen këtu.",

            ["Accept"] = "Prano",

            ["Decline"] = "Refuzo",

            ["Group invitation"] = "Ftesë për grup",


            //Professor topics(create)
            ["Create Task"] = "Krijo detyrë",
            ["Professor"] = "Profesori",
            ["Use this dedicated form to create a new assignment for students."] =
"Përdor këtë formë për të krijuar një detyrë të re për studentët.",

            ["Task"] = "Detyra",
            ["Task details"] = "Detajet e detyrës",
            ["Add the assignment instructions, deadline, and optional resources."] =
"Shto udhëzimet e detyrës, afatin dhe materialet opsionale.",

            ["Group"] = "Grupi",
            ["Choose group"] = "Zgjidh grupin",
            ["Tasks are assigned to students who belong to this group."] =
"Detyrat u caktohen studentëve që i përkasin këtij grupi.",

            ["Title"] = "Titulli",
            ["Description"] = "Përshkrimi",
            ["Deadline"] = "Afati",
            ["Optional file"] = "File opsional",
            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",

            ["Cancel"] = "Anulo",
            ["Workflow"] = "Rrjedha e punës",
            ["After creating"] = "Pas krijimit",
            ["The task appears on Manage Tasks, where it can be viewed, edited, searched, or deleted."] =
"Detyra shfaqet te Manage Tasks, ku mund të shihet, editohet, kërkohet ose fshihet.",

            ["Clean separation"] = "Ndarje e qartë",
            ["Create stays here. Management stays in the table page."] =
"Krijimi mbetet këtu. Menaxhimi mbetet në faqen e tabelës.",

            //Profesor topics(details)
            ["Task"] = "Detyra",

            ["Read-only task details and management context."] =
"Detajet e detyrës vetëm për lexim dhe konteksti i menaxhimit.",

            ["Back to Manage Tasks"] = "Kthehu te Menaxho Detyrat",

            ["Details"] = "Detajet",

            ["Completed"] = "Përfunduar",
            ["Active"] = "Aktive",

            ["Group"] = "Grupi",

            ["Unassigned"] = "E pacaktuar",

            ["Deadline"] = "Afati",

            ["Submissions"] = "Dorëzimet",

            ["Download task file"] = "Shkarko file-in e detyrës",

            ["Review"] = "Rishikim",

            ["Student submissions"] = "Dorëzimet e studentëve",

            ["submissions"] = "dorëzime",

            ["Open the submissions page to review files and feedback."] =
"Hap faqen e dorëzimeve për të parë file-t dhe feedback-un.",

            ["View Submissions"] = "Shiko dorëzimet",

            //profesor topics(index)
            ["Manage Tasks"] = "Menaxho detyrat",

            ["Task inventory"] = "Inventari i detyrave",

            ["Create tasks here, then use this table for management."] =
"Krijo detyra këtu, pastaj përdor këtë tabelë për menaxhim.",

            ["Search"] = "Kërko",

            ["Search title or description"] =
"Kërko sipas titullit ose përshkrimit",

            ["Status"] = "Statusi",

            ["All statuses"] = "Të gjitha statuset",

            ["Active"] = "Aktive",
            ["Completed"] = "Përfunduar",

            ["Apply"] = "Apliko",
            ["Reset"] = "Rivendos",

            ["No tasks found"] = "Nuk u gjetën detyra",

            ["Adjust the filters or create a task from the Tasks section."] =
"Ndrysho filtrat ose krijo një detyrë nga seksioni Detyrat.",

            ["Actions"] = "Veprimet",

            ["Manage"] = "Menaxho",

            ["Edit"] = "Ndrysho",

            ["Delete"] = "Fshij",

            ["Delete task?"] = "Ta fshijmë detyrën?",

            ["This task and its submissions will be permanently deleted. This action cannot be undone."] =
"Kjo detyrë dhe dorëzimet e saj do të fshihen përgjithmonë. Ky veprim nuk mund të kthehet.",

            ["Edit Task"] = "Ndrysho detyrën",

            ["Save"] = "Ruaj",

            ["Cancel"] = "Anulo",

            ["Choose group"] = "Zgjidh grupin",

            ["Tasks are assigned to students who belong to this group."] =
"Detyrat u caktohen studentëve që i përkasin këtij grupi.",

            ["Optional file"] = "File opsional",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",
            ["Unassigned"] = "E pacaktuar",

            //profesor TASKS(submissions)
            ["Task Submissions"] = "Dorëzimet e detyrës",

            ["Professor"] = "Profesori",

            ["Review submitted files, comments, and save feedback for {0}."] =
"Rishiko file-t e dorëzuara, komentet dhe ruaj feedback-un për {0}.",

            ["this task"] = "këtë detyrë",

            ["Back to Tasks"] = "Kthehu te Detyrat",

            ["Submissions"] = "Dorëzimet",
            ["Submission"] = "Dorëzim",

            ["Student work"] = "Puna e studentit",

            ["Feedback marks a submission as reviewed."] =
"Feedback-u e shënon dorëzimin si të rishikuar.",

            ["No submissions yet"] = "Nuk ka dorëzime ende",

            ["Student submissions for this task will appear here."] =
"Dorëzimet e studentëve për këtë detyrë do të shfaqen këtu.",

            ["Student"] = "Studenti",

            ["Submitted"] = "Dorëzuar",

            ["File name"] = "Emri i file-it",

            ["Submitted file"] = "File i dorëzuar",

            ["View File"] = "Shiko file-in",

            ["Download"] = "Shkarko",

            ["Student note"] = "Shënimi i studentit",

            ["Feedback"] = "Feedback",

            ["Reviewed"] = "Rishikuar",

            ["Needs review"] = "Ka nevojë për rishikim",

            ["Grade"] = "Nota",

            ["Reviewed without written feedback."] =
"U rishikua pa feedback të shkruar.",

            ["No feedback yet."] =
"Nuk ka feedback ende.",

            ["Edit feedback"] = "Ndrysho feedback-un",

            ["Add feedback"] = "Shto feedback",

            ["Delete feedback?"] = "Ta fshijmë feedback-un?",

            ["This feedback will be permanently deleted and the submission will return to Submitted status."] =
"Ky feedback do të fshihet përgjithmonë dhe dorëzimi do të kthehet në statusin Dorëzuar.",

            ["Delete feedback"] = "Fshij feedback-un",

            ["Review submission"] = "Rishiko dorëzimin",

            ["Write clear feedback"] = "Shkruaj feedback të qartë",

            ["Grade optional"] = "Nota opsionale",

            ["Save feedback"] = "Ruaj feedback",

            ["Cancel"] = "Anulo",

            //profesor topics batchstart
            ["Batch Topics"] = "Temat e grupit",

            ["Professor"] = "Profesori",

            ["Manage the topics inside this batch before publishing them for students."] =
"Menaxho temat brenda këtij grupi para publikimit për studentët.",

            ["Back to Batches"] = "Kthehu te grupet",

            ["Created At"] = "Krijuar më",

            ["Status"] = "Statusi",

            ["Active"] = "Aktive",
            ["Draft"] = "Draft",

            ["Topics"] = "Temat",

            ["Batch topic list"] = "Lista e temave të grupit",

            ["Review the topics in this batch and continue building it over time."] =
"Rishiko temat në këtë grup dhe vazhdo ndërtimin e tyre me kalimin e kohës.",

            ["Add Topics"] = "Shto tema",

            ["No topics in this batch yet"] =
"Nuk ka tema në këtë grup ende",

            ["This batch is ready for topic management when you add the next step."] =
"Ky grup është gati për menaxhim sapo të shtosh hapin tjetër.",

            ["Search"] = "Kërko",

            ["Search topic title"] =
"Kërko titullin e temës",

            ["All statuses"] =
"Të gjitha statuset",

            ["Available"] = "E disponueshme",

            ["Taken"] = "E zgjedhur",

            ["Reset"] = "Rivendos",

            ["Topic title"] = "Titulli i temës",

            ["Selected by"] = "Zgjedhur nga",

            ["Files"] = "File",

            ["Actions"] = "Veprimet",

            ["Edit"] = "Ndrysho",

            ["Delete"] = "Fshij",

            ["Delete topic?"] =
"Ta fshijmë temën?",

            ["This topic will be permanently deleted from the batch. This action cannot be undone."] =
"Kjo temë do të fshihet përgjithmonë nga grupi. Ky veprim nuk mund të kthehet.",

            ["Topic"] = "Tema",

            ["Edit Topic"] = "Ndrysho temën",

            ["Cancel"] = "Anulo",

            ["Save"] = "Ruaj",

            ["Topic files"] = "File-t e temës",

            ["No files attached"] =
"Nuk ka file të bashkangjitur",

            ["Add files to this specific topic when you need supporting material."] =
"Shto file në këtë temë kur të nevojiten materiale shtesë.",

            ["Add file"] = "Shto file",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",

            ["Upload file"] = "Ngarko file",

            ["Close"] = "Mbyll",

            ["One topic"] = "Një temë",

            ["Multiple topics"] = "Shumë tema",

            ["Choose topic add mode"] =
"Zgjidh mënyrën e shtimit të temave",

            ["Enter topic title"] =
"Shkruaj titullin e temës",

            ["Topic file optional"] =
"File opsional i temës",

            ["Attach one supporting file to this topic only. Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Bashkangjit vetëm një file mbështetës për këtë temë. File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",

            ["Topic titles"] =
"Titujt e temave",

            ["Write one topic per line"] =
"Shkruaj një temë në çdo rresht",

            ["Each line will be added as a separate topic."] =
"Çdo rresht do të shtohet si temë e veçantë",

            //profesor topics create
            ["Create Batch"] = "Krijo grup",

            ["Professor"] = "Profesori",

            ["Create a focused topic batch by adding a title and listing topics one per line."] =
"Krijo një grup temash duke shtuar titull dhe duke listuar temat një në çdo rresht.",

            ["Topics"] = "Temat",

            ["Batch details"] =
"Detajet e grupit",

            ["Add a batch name and provide each topic on its own line."] =
"Shto emrin e grupit dhe shkruaj secilën temë në rresht të veçantë.",

            ["Topic batch title"] =
"Titulli i grupit të temave",

            ["Write one topic per line."] =
"Shkruaj një temë në çdo rresht.",

            ["Cancel"] = "Anulo",

            ["Preview"] = "Parashikim",

            ["Review before saving"] =
"Rishiko para ruajtjes",

            ["No topics yet"] =
"Nuk ka tema ende",

            ["No topics to preview"] =
"Nuk ka tema për parashikim",

            ["Paste topics to see the list here."] =
"Ngjit temat për ta parë listën këtu.",

            //profesor topics index
            ["Manage Topics"] = "Menaxho temat",

            ["Topic Batches"] = "Grupet e temave",

            ["Your topic inventory"] =
"Inventari yt i temave",

            ["Create topic lists here, then manage existing batches in the table."] =
"Krijo lista temash këtu, pastaj menaxho grupet ekzistuese në tabelë.",

            ["Create Batch"] =
"Krijo grup",

            ["Search"] =
"Kërko",

            ["Search batch or topic title"] =
"Kërko grupin ose titullin e temës",

            ["Status"] =
"Statusi",

            ["All statuses"] =
"Të gjitha statuset",

            ["Active"] =
"Aktive",

            ["Draft"] =
"Draft",

            ["Apply"] =
"Apliko",

            ["Reset"] =
"Rivendos",

            ["No topic batches found"] =
"Nuk u gjetën grupe temash",

            ["Adjust the filters or create a new topic list from the Topics section."] =
"Ndrysho filtrat ose krijo një listë të re temash nga seksioni Temat.",

            ["Topic batch"] =
"Grupi i temave",

            ["Professor"] =
"Profesori",

            ["Topics"] =
"Temat",

            ["Actions"] =
"Veprimet",

            ["Created"] =
"Krijuar",

            ["total"] =
"gjithsej",

            ["available"] =
"të disponueshme",

            ["taken"] =
"të zgjedhura",

            ["Manage"] =
"Menaxho",

            ["Activate"] =
"Aktivizo",

            ["Edit"] =
"Ndrysho",

            ["Delete"] =
"Fshij",

            ["Delete topic batch?"] =
"Ta fshijmë grupin e temave?",

            ["This topic batch and its topics will be permanently deleted. This action cannot be undone."] =
"Ky grup temash dhe temat e tij do të fshihen përgjithmonë. Ky veprim nuk mund të kthehet.",

            ["Edit Topic Batch"] =
"Ndrysho grupin e temave",

            ["Title"] =
"Titulli",

            ["Cancel"] =
"Anulo",

            ["Save"] =
"Ruaj",

            ["Activate Batch"] =
"Aktivizo grupin",

            ["Make this batch visible?"] =
"Ta bëjmë këtë grup të dukshëm?",

            ["Students will be able to view this batch and select available topics after activation."] =
"Studentët do të mund ta shohin këtë grup dhe të zgjedhin temat e disponueshme pas aktivizimit.",

            ["Topic batch title"] =
"Titulli i grupit të temave",

            ["Write one topic per line. New batches start as Draft and stay hidden from students until activated."] =
"Shkruaj një temë në çdo rresht. Grupet e reja fillojnë si Draft dhe mbeten të fshehura derisa të aktivizohen.",

            ["Preview"] =
"Parashikim",

            ["Review before saving"] =
"Rishiko para ruajtjes",

            ["No topics yet"] =
"Nuk ka tema ende",

            ["No topics to preview"] =
"Nuk ka tema për parashikim",

            ["Paste topics into the create modal to see them here."] =
"Ngjit temat në modalin e krijimit për t’i parë këtu.",

            //settings
            ["Please fix the following issues:"] =
"Ju lutem rregulloni problemet e mëposhtme:",

            ["Account"] =
"Llogaria",

            ["Account profile"] =
"Profili i llogarisë",

            ["Manage the identity shown across your workspace."] =
"Menaxho identitetin që shfaqet në hapësirën tënde të punës.",

            ["Change profile photo"] =
"Ndrysho fotografinë",

            ["Change photo"] =
"Ndrysho foton",

            ["Full Name"] =
"Emri i plotë",

            ["Enter your full name"] =
"Shkruaj emrin e plotë",

            ["Email"] =
"Email",

            ["Email is locked for account security."] =
"Email-i është i bllokuar për arsye sigurie.",

            ["Role"] =
"Roli",

            ["Role is managed by the account type."] =
"Roli menaxhohet nga tipi i llogarisë.",

            ["Save changes"] =
"Ruaj ndryshimet",

            ["Preferences"] =
"Preferencat",

            ["Visual mode"] =
"Pamja",

            ["Switch between a bright productivity setup and a darker, focused look."] =
"Ndrysho mes një pamjeje të ndritshme dhe një pamjeje më të errët e të fokusuar.",

            ["Light"] =
"E çelët",

            ["Bright cards and crisp contrast for daytime work."] =
"Karta të ndritshme dhe kontrast i pastër për punë gjatë ditës.",

            ["Dark"] =
"E errët",

            ["Deeper surfaces for late sessions and reduced glare."] =
"Sipërfaqe më të errëta për sesione të vona dhe më pak lodhje të syve.",

            ["Language"] =
"Gjuha",

            ["Interface language"] =
"Gjuha e ndërfaqes",

            ["Choose the language you want to use when language support is enabled."] =
"Zgjidh gjuhën që dëshiron të përdorësh.",

            ["shqip"] =
"Shqip",

            ["english"] =
"Anglisht",

            ["Save language"] =
"Ruaj gjuhën",

            ["Password"] =
"Fjalëkalimi",

            ["Change password"] =
"Ndrysho fjalëkalimin",

            ["Verify your current password before saving a new one."] =
"Verifiko fjalëkalimin aktual para ruajtjes së të riut.",

            ["Current password"] =
"Fjalëkalimi aktual",

            ["New password"] =
"Fjalëkalimi i ri",

            ["Confirm password"] =
"Konfirmo fjalëkalimin",

            ["Update password"] =
"Përditëso fjalëkalimin",

            ["Danger Zone"] =
"Zona e rrezikut",

            ["Delete account"] =
"Fshi llogarinë",

            ["Permanent"] =
"Përhershme",

            ["This permanently removes your account and clears the current session."] =
"Kjo e fshin përgjithmonë llogarinë dhe e mbyll sesionin aktual.",

            ["Delete account?"] =
"Ta fshijmë llogarinë?",

            ["This account will be permanently deleted. Your access will be removed immediately and you will be signed out."] =
"Kjo llogari do të fshihet përgjithmonë. Qasja do të hiqet menjëherë dhe do të çkyçesh.",

            ["Deleting account..."] =
"Duke fshirë llogarinë...",

            ["Delete my account"] =
"Fshi llogarinë time",

            //dashboard
            ["Workspace"] = "Hapësira e punës",

            ["Dashboard"] = "Paneli",

            ["Topics"] = "Temat",

            ["Available Topics"] = "Temat e disponueshme",

            ["My Topic"] = "Tema ime",

            ["Tasks"] = "Detyrat",

            ["Groups"] = "Grupet",

            ["Settings"] = "Cilësimet",

            ["Log out"] = "Dil",
            ["Notifications"] = "Njoftimet",
            ["Loading"] = "Duke u ngarkuar",
            ["Loading updates..."] = "Duke ngarkuar përditësimet...",
            ["Professor"] = "Profesori",
            ["Student"] = "Studenti",
            ["Faculty Projects"] = "Projektet e fakultetit",
            ["Review groups after you request to join or after your request is accepted."] =
"Rishiko grupet pasi të kërkosh bashkim ose pasi kërkesa jote të pranohet.",
            ["Search for professors in Groups and send a join request. Requested or accepted groups will appear here."] =
"Kërko profesorë te Grupet dhe dërgo kërkesë për bashkim. Grupet e kërkuara ose të pranuara do të shfaqen këtu.",
            ["Professor not assigned"] = "Profesori nuk është caktuar",
            ["Open spots"] = "Vende të lira",
            ["Your request is pending. Topics become active after acceptance."] =
"Kërkesa jote është në pritje. Temat bëhen aktive pas pranimit.",
            ["Your group is active. Review your selected topic and available options."] =
"Grupi yt është aktiv. Rishiko temën e zgjedhur dhe opsionet e disponueshme.",
            ["Download file"] = "Shkarko file-in",
            ["Request pending"] = "Kërkesa në pritje",
            ["This group is visible because you sent a join request. It will become active when the professor accepts you into the group."] =
"Ky grup është i dukshëm sepse ke dërguar kërkesë për bashkim. Do të bëhet aktiv kur profesori të të pranojë në grup.",
            ["Already selected"] = "Tashmë e zgjedhur",
            ["Select Topic"] = "Zgjidh temën",
            ["Filled"] = "E mbushur",
            ["file"] = "file",
            ["files"] = "file",

            //delete modal
            ["Permanent Delete"] = "Fshirje e përhershme",

            ["Delete item?"] = "Ta fshijmë këtë?",

            ["This item will be permanently deleted. This action cannot be undone."] =
"Ky element do të fshihet përgjithmonë. Ky veprim nuk mund të kthehet.",

            ["Cancel"] = "Anulo",

            ["Delete"] = "Fshij",

            ["Close"] = "Mbyll",

            //LAYOUT
            ["Features"] = "Veçoritë",
            ["How It Works"] = "Si funksionon",
            ["Benefits"] = "Përfitimet",
            ["Login"] = "Kyçu",
            ["Register"] = "Regjistrohu",
            ["Real-time academic collaboration for students and professors, built for clarity, structure, and progress."] =
"Bashkëpunim akademik në kohë reale për studentë dhe profesorë, i ndërtuar për qartësi, strukturë dhe progres.",
            ["Home"] = "Ballina",
            ["Privacy"] = "Privatësia",

            //FOTO MODAL
            ["CollabSpace User"] = "Përdorues i CollabSpace",

            ["Close photo modal"] = "Mbyll modalin e fotos",

            ["Profile Photo"] = "Foto e profilit",

            ["Update your account image"] = "Përditëso foton e llogarisë",

            ["Choose a new image from your device or reset back to the default profile picture."] =
"Zgjidh një foto të re nga pajisja ose kthehu te fotoja e parazgjedhur e profilit.",

            ["Choose image"] = "Zgjidh foto",

            ["JPG, PNG, GIF, or WEBP up to 5 MB."] =
"JPG, PNG, GIF ose WEBP deri në 5 MB.",

            ["Upload Photo"] = "Ngarko foto",

            ["Reset to Default"] = "Kthe në të parazgjedhurën",

            ["Account settings"] = "Cilësimet e llogarisë",

            ["current profile photo"] = "foto aktuale e profilit",

            //settings section
            ["Please fix the following issues:"] = "Ju lutem rregulloni problemet e mëposhtme:",

            ["Account"] = "Llogaria",
            ["Account profile"] = "Profili i llogarisë",
            ["Manage the identity shown across your workspace."] =
"Menaxho identitetin që shfaqet në hapësirën tënde të punës.",

            ["Change profile photo"] = "Ndrysho fotografinë",
            ["Change photo"] = "Ndrysho foton",

            ["Full Name"] = "Emri i plotë",
            ["Enter your full name"] = "Shkruaj emrin e plotë",

            ["Email"] = "Email",
            ["Email is locked for account security."] =
"Email-i është i bllokuar për arsye sigurie.",

            ["Role"] = "Roli",
            ["Role is managed by the account type."] =
"Roli menaxhohet nga tipi i llogarisë.",

            ["Save changes"] = "Ruaj ndryshimet",

            ["Preferences"] = "Preferencat",

            ["Visual mode"] = "Pamja",

            ["Switch between a bright productivity setup and a darker, focused look."] =
"Ndrysho mes një pamjeje të ndritshme dhe një pamjeje më të errët e të fokusuar.",

            ["Light"] = "E çelët",

            ["Bright cards and crisp contrast for daytime work."] =
"Karta të ndritshme dhe kontrast i pastër për punë gjatë ditës.",

            ["Dark"] = "E errët",

            ["Deeper surfaces for late sessions and reduced glare."] =
"Sipërfaqe më të errëta për sesione të vona dhe më pak lodhje të syve.",

            ["Language"] = "Gjuha",

            ["Interface language"] = "Gjuha e ndërfaqes",

            ["Choose the language you want to use when language support is enabled."] =
"Zgjidh gjuhën që dëshiron të përdorësh.",

            ["shqip"] = "Shqip",
            ["english"] = "Anglisht",

            ["Save language"] = "Ruaj gjuhën",

            ["Password"] = "Fjalëkalimi",

            ["Change password"] = "Ndrysho fjalëkalimin",

            ["Verify your current password before saving a new one."] =
"Verifiko fjalëkalimin aktual para ruajtjes së të riut.",

            ["Current password"] = "Fjalëkalimi aktual",

            ["Show current password"] =
"Shfaq fjalëkalimin aktual",

            ["New password"] = "Fjalëkalimi i ri",

            ["Show new password"] =
"Shfaq fjalëkalimin e ri",

            ["Confirm password"] = "Konfirmo fjalëkalimin",

            ["Show password confirmation"] =
"Shfaq konfirmimin e fjalëkalimit",

            ["Update password"] = "Përditëso fjalëkalimin",

            ["Danger Zone"] = "Zona e rrezikut",

            ["Delete account"] = "Fshi llogarinë",

            ["Permanent"] = "Përhershme",

            ["This permanently removes your account and clears the current session."] =
"Kjo e fshin përgjithmonë llogarinë dhe e mbyll sesionin aktual.",

            ["Delete account?"] =
"Ta fshijmë llogarinë?",

            ["This account will be permanently deleted. Your access will be removed immediately and you will be signed out."] =
"Kjo llogari do të fshihet përgjithmonë. Qasja do të hiqet menjëherë dhe do të çkyçesh.",

            ["Deleting account..."] =
"Duke fshirë llogarinë...",

            ["Delete my account"] =
"Fshi llogarinë time",

            ["profile photo"] =
"foto e profilit",

            //details
            ["Submitted"] = "Dorëzuar",

            ["Deadline Passed"] = "Afati ka kaluar",

            ["Active"] = "Aktive",

            ["submitted"] = "dorëzuar",

            ["deadline-passed"] = "afat-i-kaluar",

            ["active"] = "aktive",

            ["Reviewed"] = "Rishikuar",

            ["Student Task"] = "Detyra e studentit",

            ["Review the task details and submit your completed work when it is ready."] =
"Rishiko detajet e detyrës dhe dorëzo punën kur të jetë gati.",

            ["Back to tasks"] =
"Kthehu te detyrat",

            ["Task Details"] =
"Detajet e detyrës",

            ["Professor"] =
"Profesori",

            ["Group"] =
"Grupi",

            ["Deadline"] =
"Afati",

            ["Open guidance link"] =
"Hap linkun udhëzues",

            ["Download task file"] =
"Shkarko file-in e detyrës",

            ["Submission"] =
"Dorëzimi",

            ["Your work"] =
"Puna jote",

            ["File"] =
"File",

            ["Comment"] =
"Koment",

            ["Professor feedback"] =
"Feedback nga profesori",

            ["Grade:"] =
"Nota:",

            ["Deadline passed"] =
"Afati ka kaluar",

            ["Submissions and changes are closed for this task."] =
"Dorëzimet dhe ndryshimet janë mbyllur për këtë detyrë.",

            ["Completed work file"] =
"File i punës së përfunduar",

            ["Upload improved file"] =
"Ngarko versionin e përmirësuar",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",

            ["Comment optional"] =
"Koment opsional",

            ["Add a short note for the professor"] =
"Shto një shënim të shkurtër për profesorin",

            ["Submit Work"] =
"Dorëzo punën",

            ["Update Submission"] =
"Përditëso dorëzimin",

            ["Delete submission?"] =
"Ta fshijmë dorëzimin?",

            ["This submission will be permanently deleted. This action cannot be undone."] =
"Ky dorëzim do të fshihet përgjithmonë. Ky veprim nuk mund të kthehet.",

            ["Delete submission"] =
"Fshij dorëzimin",

            //student tasks index
            ["Student"] = "Studenti",

            ["Tasks"] = "Detyrat",

            ["Review assigned tasks, deadlines, and any meeting or guidance links shared by professors."] =
"Rishiko detyrat e caktuara, afatet dhe çdo link takimi ose udhëzues të ndarë nga profesorët.",

            ["No tasks available"] =
"Nuk ka detyra të disponueshme",

            ["Group tasks will appear here after your membership is accepted."] =
"Detyrat e grupit do të shfaqen këtu pasi anëtarësimi yt të pranohet.",

            ["Group"] =
"Grupi",

            ["Task"] =
"Detyra",

            ["Submitted"] =
"Dorëzuar",

            ["Deadline Passed"] =
"Afati ka kaluar",

            ["Active"] =
"Aktive",

            ["submitted"] =
"dorëzuar",

            ["deadline-passed"] =
"afat-i-kaluar",

            ["active"] =
"aktive",

            ["Reviewed"] =
"Rishikuar",

            ["Student Task"] =
"Detyra e studentit",

            ["Deadline"] =
"Afati",

            ["Professor"] =
"Profesori",

            ["Download task file"] =
"Shkarko file-in e detyrës",

            ["No task file attached"] =
"Nuk ka file të bashkangjitur",

            ["Open guidance link"] =
"Hap linkun udhëzues",

            ["Submit Work"] =
"Dorëzo punën",

            ["Submissions are closed."] =
"Dorëzimet janë mbyllur.",

            ["Submitted file"] =
"File i dorëzuar",

            ["Comment"] =
"Koment",

            ["Grade"] =
"Nota",

            ["Submission changes are closed."] =
"Ndryshimet e dorëzimit janë mbyllur.",

            ["Update Submission"] =
"Përditëso dorëzimin",

            ["Delete submission?"] =
"Ta fshijmë dorëzimin?",

            ["This submission will be permanently deleted. This action cannot be undone."] =
"Ky dorëzim do të fshihet përgjithmonë. Ky veprim nuk mund të kthehet.",

            ["Delete submission"] =
"Fshij dorëzimin",

            ["Completed work file"] =
"File i punës së përfunduar",

            ["Upload improved file"] =
"Ngarko versionin e përmirësuar",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",

            ["Add a short note for the professor"] =
"Shto një shënim të shkurtër për profesorin",

            ["Cancel"] =
"Anulo",

            //student tasks submited task
            ["Student"] = "Studenti",
            ["Tasks"] = "Detyrat",
            ["Review assigned tasks, deadlines, and any meeting or guidance links shared by professors."] =
"Rishiko detyrat e caktuara, afatet dhe çdo link takimi ose udhëzues të ndarë nga profesorët.",
            ["No tasks available"] = "Nuk ka detyra të disponueshme",
            ["Group tasks will appear here after your membership is accepted."] =
"Detyrat e grupit do të shfaqen këtu pasi anëtarësimi yt të pranohet.",
            ["Group"] = "Grupi",
            ["Task"] = "Detyra",
            ["Submitted"] = "Dorëzuar",
            ["Deadline Passed"] = "Afati ka kaluar",
            ["Active"] = "Aktive",
            ["Reviewed"] = "Rishikuar",
            ["Student Task"] = "Detyra e studentit",
            ["Deadline"] = "Afati",
            ["Professor"] = "Profesori",
            ["Download task file"] = "Shkarko file-in e detyrës",
            ["No task file attached"] = "Nuk ka file të bashkangjitur",
            ["Open guidance link"] = "Hap linkun udhëzues",
            ["Submit Work"] = "Dorëzo punën",
            ["Submissions are closed."] = "Dorëzimet janë mbyllur.",
            ["Submitted file"] = "File i dorëzuar",
            ["Comment"] = "Koment",
            ["Professor feedback"] = "Feedback nga profesori",
            ["Grade"] = "Nota",
            ["Submission changes are closed."] = "Ndryshimet e dorëzimit janë mbyllur.",
            ["Update Submission"] = "Përditëso dorëzimin",
            ["Delete submission?"] = "Ta fshijmë dorëzimin?",
            ["This submission will be permanently deleted. This action cannot be undone."] =
"Ky dorëzim do të fshihet përgjithmonë. Ky veprim nuk mund të kthehet.",
            ["Delete submission"] = "Fshij dorëzimin",
            ["Completed work file"] = "File i punës së përfunduar",
            ["Upload improved file"] = "Ngarko versionin e përmirësuar",
            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"File të mbështetura: PDF, DOCX, ZIP, PNG, JPG.",
            ["Comment optional"] = "Koment opsional",
            ["Add a short note for the professor"] = "Shto një shënim të shkurtër për profesorin",
            ["Cancel"] = "Anulo",
            ["Close"] = "Mbyll",

            //student topic index

            //mytopic


            // Sidebar
            ["Dashboard"] = "Paneli",
            ["Topics"] = "Temat",
            ["Tasks"] = "Detyrat",
            ["Groups"] = "Grupet",
            ["Notifications"] = "Njoftimet",
            ["Settings"] = "Cilësimet",
            ["Logout"] = "Dil",
            ["Workspace"] = "Hapësira e Punës"

        };

        private static readonly Dictionary<string, string> En = new()
        {
            //Homepage
            ["Features"] = "Features",
            ["How It Works"] = "How It Works",
            ["Benefits"] = "Benefits",

            ["Login"] = "Login",
            ["Register"] = "Register",
            ["Welcome back"] = "Welcome back",
            ["Name"] = "Name",
            ["Enter your email"] = "Enter your email",
            ["Enter your password"] = "Enter your password",
            ["Log In"] = "Log In",
            ["Don't have an account?"] = "Don't have an account?",
            ["Create account"] = "Create account",
            ["Forgot your password?"] = "Forgot your password?",
            ["Reset it"] = "Reset it",
            ["Create your account"] = "Create your account",
            ["Enter your name"] = "Enter your name",
            ["Enter a password"] = "Enter a password",
            ["Confirm Password"] = "Confirm Password",
            ["Repeat your password"] = "Repeat your password",
            ["Create Account"] = "Create Account",
            ["Already have an account?"] = "Already have an account?",
            ["Log in"] = "Log in",
            ["Reset your password"] = "Reset your password",
            ["Continue"] = "Continue",
            ["Back to Login"] = "Back to Login",
            ["Set your new password"] = "Set your new password",
            ["Enter your new password"] = "Enter your new password",
            ["Repeat your new password"] = "Repeat your new password",

            ["Home"] = "Home",
            ["Privacy"] = "Privacy",

            ["Real-time academic collaboration for students and professors, built for clarity, structure, and progress."] =
            "Real-time academic collaboration for students and professors, built for clarity, structure, and progress.",

            ["Current focus"] = "Current focus",
            ["Core capabilities"] = "Core capabilities",

            ["Real-time academic collaboration platform"] = "Real-time academic collaboration platform",
            ["CollabSpace brings students and professors into one structured, live academic workspace."] = "CollabSpace brings students and professors into one structured, live academic workspace.",
            ["Manage project topics, tasks, quizzes, teamwork, progress, and professor-student coordination in a platform designed for clarity and real-time momentum."] = "Manage project topics, tasks, quizzes, teamwork, progress, and professor-student coordination in a platform designed for clarity and real-time momentum.",

            ["Explore Features"] = "Explore Features",
            ["See How It Works"] = "See How It Works",

            ["Role-aware coordination"] = "Role-aware coordination",
            ["Designed for both students and professors"] = "Designed for both students and professors",

            ["Live academic workflow"] = "Live academic workflow",
            ["Tasks, progress, and updates stay connected"] = "Tasks, progress, and updates stay connected",

            ["Built for students and professors"] = "Built for students and professors",
            ["Organized academic workflow"] = "Organized academic workflow",
            ["Live updates and shared progress"] = "Live updates and shared progress",

            ["CollabSpace Workspace"] = "CollabSpace Workspace",
            ["Workspace preview"] = "Workspace preview",
            ["Academic coordination, in one place"] = "Academic coordination, in one place",

            ["Live sync"] = "Live sync",

            ["Realtime"] = "Realtime",
            ["Team activity"] = "Team activity",

            ["Structured"] = "Structured",
            ["Task flow"] = "Task flow",

            ["Visible"] = "Visible",
            ["Progress + feedback"] = "Progress + feedback",

            ["One organized view for collaboration, planning, and progress."] = "One organized view for collaboration, planning, and progress.",
            ["CollabSpace keeps topic decisions, tasks, guidance, quizzes, and updates in one connected academic environment."] = "CollabSpace keeps topic decisions, tasks, guidance, quizzes, and updates in one connected academic environment.",

            ["Real-time teamwork spaces"] = "Real-time teamwork spaces",
            ["Shared work remains visible while groups collaborate and adjust quickly."] = "Shared work remains visible while groups collaborate and adjust quickly.",

            ["Tasks, topics, and quizzes"] = "Tasks, topics, and quizzes",
            ["Academic work stays structured from early planning through evaluation."] = "Academic work stays structured from early planning through evaluation.",

            ["Role-based guidance and visibility"] = "Role-based guidance and visibility",
            ["Students and professors get the clarity they need without added noise."] = "Students and professors get the clarity they need without added noise.",

            ["Project alignment"] = "Project alignment",
            ["Topic review, task assignment, and professor feedback remain synchronized."] = "Topic review, task assignment, and professor feedback remain synchronized.",

            ["Today"] = "Today",

            ["Task updates published live"] = "Task updates published live",
            ["Quiz checkpoints ready"] = "Quiz checkpoints ready",
            ["Professor guidance attached to work"] = "Professor guidance attached to work",

            ["A curated feature set built around the way academic collaboration actually works."] = "A curated feature set built around the way academic collaboration actually works.",
            ["Instead of scattered tools and disconnected actions, CollabSpace organizes the most important parts of academic teamwork into a clear, focused experience."] = "Instead of scattered tools and disconnected actions, CollabSpace organizes the most important parts of academic teamwork into a clear, focused experience.",

            ["Live collaboration"] = "Live collaboration",
            ["Coordinate teamwork in real time without losing structure."] = "Coordinate teamwork in real time without losing structure.",
            ["Students and professors can follow activity as it happens while keeping discussions, tasks, and project movement inside an organized academic flow."] = "Students and professors can follow activity as it happens while keeping discussions, tasks, and project movement inside an organized academic flow.",

            ["Project planning"] = "Project planning",
            ["From topic selection to task management"] = "From topic selection to task management",
            ["Move from choosing project directions to assigning and managing work with better visibility and less confusion."] = "Move from choosing project directions to assigning and managing work with better visibility and less confusion.",

            ["Evaluation and progress"] = "Evaluation and progress",
            ["Track quizzes, milestones, and outcomes clearly"] = "Track quizzes, milestones, and outcomes clearly",
            ["Keep academic progress easy to monitor with structured checkpoints, quizzes, and readable progress signals."] = "Keep academic progress easy to monitor with structured checkpoints, quizzes, and readable progress signals.",

            ["Role-aware workflow"] = "Role-aware workflow",
            ["Built for students and professors together"] = "Built for students and professors together",
            ["Role-based access, clearer responsibilities, and professor-student interaction make the platform feel more intentional and easier to use."] = "Role-based access, clearer responsibilities, and professor-student interaction make the platform feel more intentional and easier to use.",

            ["Topic selection"] = "Topic selection",
            ["Task ownership"] = "Task ownership",
            ["Teamwork spaces"] = "Teamwork spaces",
            ["Professor interaction"] = "Professor interaction",
            ["Live notifications"] = "Live notifications",
            ["Progress tracking"] = "Progress tracking",

            ["How it works"] = "How it works",
            ["A clear path from account creation to live academic coordination."] = "A clear path from account creation to live academic coordination.",
            ["The workflow stays simple and readable so users understand the platform quickly and move into meaningful work without friction."] = "The workflow stays simple and readable so users understand the platform quickly and move into meaningful work without friction.",

            ["Create an account"] = "Create an account",
            ["Students and professors enter through a straightforward account setup experience."] = "Students and professors enter through a straightforward account setup experience.",

            ["Enter with the right role"] = "Enter with the right role",
            ["Each user lands in a role-aware environment shaped around academic responsibilities."] = "Each user lands in a role-aware environment shaped around academic responsibilities.",

            ["Manage or join academic work"] = "Manage or join academic work",
            ["Start projects, choose topics, form groups, and organize the work that needs to happen."] = "Start projects, choose topics, form groups, and organize the work that needs to happen.",

            ["Collaborate in real time"] = "Collaborate in real time",
            ["Keep communication, tasks, decisions, and guidance moving inside one shared space."] = "Keep communication, tasks, decisions, and guidance moving inside one shared space.",

            ["Follow progress and evaluation"] = "Follow progress and evaluation",
            ["Track quizzes, milestones, and updates so momentum stays visible and manageable."] = "Track quizzes, milestones, and updates so momentum stays visible and manageable.",

            ["Why CollabSpace"] = "Why CollabSpace",
            ["A more organized academic environment leads to better coordination and fewer lost details."] = "A more organized academic environment leads to better coordination and fewer lost details.",
            ["CollabSpace is valuable because it makes academic collaboration easier to understand, easier to manage, and easier to keep moving."] = "CollabSpace is valuable because it makes academic collaboration easier to understand, easier to manage, and easier to keep moving.",

            ["Centralized collaboration"] = "Centralized collaboration",
            ["Project work, communication, feedback, and progress stay connected instead of scattered across separate tools."] = "Project work, communication, feedback, and progress stay connected instead of scattered across separate tools.",

            ["Clearer structure"] = "Clearer structure",
            ["Students and professors can see responsibilities, progress, and next steps with much less ambiguity."] = "Students and professors can see responsibilities, progress, and next steps with much less ambiguity.",

            ["Platform value"] = "Platform value",
            ["Why teams work better with a shared academic system"] = "Why teams work better with a shared academic system",

            ["Better organization across the full workflow"] = "Better organization across the full workflow",
            ["From topic selection to task completion, the platform keeps academic work connected in one clear path."] = "From topic selection to task completion, the platform keeps academic work connected in one clear path.",

            ["Faster communication with less friction"] = "Faster communication with less friction",
            ["Professor-student interaction and live notifications make it easier to respond at the right time."] = "Professor-student interaction and live notifications make it easier to respond at the right time.",

            ["Clearer progress and more efficient management"] = "Clearer progress and more efficient management",
            ["Tasks, quizzes, milestones, and live updates help teams stay aligned without extra overhead."] = "Tasks, quizzes, milestones, and live updates help teams stay aligned without extra overhead.",

            ["Get started"] = "Get started",
            ["Bring structure, live coordination, and academic clarity into one premium platform."] = "Bring structure, live coordination, and academic clarity into one premium platform.",
            ["Sign in to continue your work or create an account to start using CollabSpace with your students, professors, and teams."] = "Sign in to continue your work or create an account to start using CollabSpace with your students, professors, and teams.",

            //Privacy
            ["Privacy Policy"] = "Privacy Policy",

            ["Using this page to detail the site's privacy policy."] =
             "Using this page to detail the site's privacy policy.",

            //Sidebar Groups
            ["Groups"] = "Groups",
            ["Group"] = "Group",
            ["Joined"] = "Joined",
            ["Requested"] = "Requested",
            ["Open"] = "Open",
            ["Active"] = "Active",
            ["Draft"] = "Draft",
            ["Search"] = "Search",
            ["Clear"] = "Clear",

            ["No groups available"] = "No groups available",
            ["No groups found"] = "No groups found",
            ["Groups created by professors will appear here."] = "Groups created by professors will appear here.",
            ["Try another group name, professor, university, or field."] = "Try another group name, professor, university, or field.",

            ["capacity"] = "capacity",
            ["open spots"] = "open spots",
            ["filled"] = "filled",
            ["created"] = "created",
            ["students"] = "students",

            ["Email not provided"] = "Email not provided",
            ["University not provided"] = "University not provided",
            ["Field not provided"] = "Field not provided",
            ["Selected"] = "Selected",

            ["View Topics"] = "View Topics",
            ["Your join request is pending."] = "Your join request is pending.",
            ["Request to Join Group"] = "Request to Join Group",

            ["Find groups"] = "Find groups",
            ["Search by group, professor, university, field, or topic."] = "Search by group, professor, university, field, or topic.",
            ["Search groups"] = "Search groups",
            ["Group or professor..."] = "Group or professor...",
            ["Newest first"] = "Newest first",
            ["Ranked results"] = "Ranked results",
            ["All professor groups are sorted from newest to oldest."] = "All professor groups are sorted from newest to oldest.",
            ["Matches are ranked by the closest group and professor details."] = "Matches are ranked by the closest group and professor details.",

            ["Topic groups"] = "Topic groups",
            ["Organized topic ownership"] = "Organized topic ownership",
            ["Use the Topics section to review available topic groups and your current selection."] = "Use the Topics section to review available topic groups and your current selection.",

            ["No groups yet"] = "No groups yet",
            ["Create a group first, then student membership will appear here."] = "Create a group first, then student membership will appear here.",
            ["No students in this group"] = "No students in this group",
            ["Search registered students and add them to this group."] = "Search registered students and add them to this group.",

            ["Students"] = "Students",
            ["Find registered students"] = "Find registered students",
            ["Search by name, email, university, or field of study, then add the student to a group."] = "Search by name, email, university, or field of study, then add the student to a group.",
            ["Search students"] = "Search students",
            ["Name, email, university..."] = "Name, email, university...",
            ["Start with a search"] = "Start with a search",
            ["Student preview cards will appear here."] = "Student preview cards will appear here.",
            ["No students found"] = "No students found",
            ["Try another name, email, university, or field."] = "Try another name, email, university, or field.",
            ["Add to group"] = "Add to group",
            ["No groups with open spots"] = "No groups with open spots",
            ["Add Student"] = "Add Student",

            ["Requests"] = "Requests",
            ["Join requests"] = "Join requests",
            ["Pending requests for your groups."] = "Pending requests for your groups.",
            ["No pending requests"] = "No pending requests",
            ["Student requests to join your groups will appear here."] = "Student requests to join your groups will appear here.",
            ["Accept"] = "Accept",
            ["Deny"] = "Deny",

            //Index 
            ["Dashboard"] = "Dashboard",

            ["Published groups"] = "Published groups",
            ["Total topics"] = "Total topics",
            ["Pending submissions"] = "Pending submissions",
            ["Upcoming deadlines"] = "Upcoming deadlines",

            ["Selected topics"] = "Selected topics",
            ["Assigned tasks"] = "Assigned tasks",
            ["Pending tasks"] = "Pending tasks",

            ["Project Topics"] = "Project Topics",
            ["Selected Topic"] = "Selected Topic",
            ["Manage topics"] = "Manage topics",

            ["No topic selected"] = "No topic selected",
            ["Select a topic to show it here."] = "Select a topic to show it here.",

            ["Deadlines"] = "Deadlines",
            ["Manage tasks"] = "Manage tasks",

            ["No upcoming deadlines"] = "No upcoming deadlines",
            ["Future task deadlines will appear here."] = "Future task deadlines will appear here.",

            ["Activity"] = "Activity",
            ["Recent Activity"] = "Recent Activity",

            ["No recent activity"] = "No recent activity",

            ["New submissions, topic selections, and feedback updates will appear here."] =
            "New submissions, topic selections, and feedback updates will appear here.",

            //Projects
            ["Projects"] = "Projects",

            ["Topic alignment"] = "Topic alignment",

            ["Shared project direction"] = "Shared project direction",

            ["Keep titles, scope, deliverables, and team ownership clearly documented."] =
            "Keep titles, scope, deliverables, and team ownership clearly documented.",

            ["Progress"] = "Progress",

            ["Milestones and deadlines"] = "Milestones and deadlines",

            ["Track what is on schedule, what needs review, and what is ready to move forward."] =
            "Track what is on schedule, what needs review, and what is ready to move forward.",

            //Settings
            ["Settings"] = "Settings",

            ["Account"] = "Account",

            ["Profile details"] = "Profile details",

            ["Keep personal information, language preferences, and workspace identity consistent."] =
             "Keep personal information, language preferences, and workspace identity consistent.",

            ["Security"] = "Security",

            ["Access and protection"] = "Access and protection",

            ["Use account-related settings to support secure access alongside the current session-based sign-in flow."] =
            "Use account-related settings to support secure access alongside the current session-based sign-in flow.",

            //notifications
            ["Notifications"] = "Notifications",

            ["System updates"] = "System updates",

            ["Important updates about groups, tasks, deadlines, and feedback."] =
            "Important updates about groups, tasks, deadlines, and feedback.",

            ["Group invitation"] = "Group invitation",

            ["Invite a student"] = "Invite a student",

            ["Student"] = "Student",

            ["Select student"] = "Select student",

            ["Topic group"] = "Topic group",

            ["Select available topic"] = "Select available topic",

            ["Send invitation"] = "Send invitation",

            ["Invitation status"] = "Invitation status",

            ["Sent invitations"] = "Sent invitations",

            ["No invitations sent"] = "No invitations sent",

            ["Group invitations and their status will appear here."] =
            "Group invitations and their status will appear here.",

            ["Pending"] = "Pending",

            ["Updates"] = "Updates",

            ["Important notifications"] = "Important notifications",

            ["No notifications"] = "No notifications",

            ["Important system updates will appear here."] =
            "Important system updates will appear here.",

            ["Accept"] = "Accept",

            ["Decline"] = "Decline",

            //Professor TASKS (create)
            ["Create Task"] = "Create Task",
            ["Professor"] = "Professor",
            ["Use this dedicated form to create a new assignment for students."] =
"Use this dedicated form to create a new assignment for students.",

            ["Task"] = "Task",
            ["Task details"] = "Task details",
            ["Add the assignment instructions, deadline, and optional resources."] =
"Add the assignment instructions, deadline, and optional resources.",

            ["Group"] = "Group",
            ["Choose group"] = "Choose group",
            ["Tasks are assigned to students who belong to this group."] =
"Tasks are assigned to students who belong to this group.",

            ["Title"] = "Title",
            ["Description"] = "Description",
            ["Deadline"] = "Deadline",
            ["Optional file"] = "Optional file",
            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Supported files: PDF, DOCX, ZIP, PNG, JPG.",

            ["Cancel"] = "Cancel",
            ["Workflow"] = "Workflow",
            ["After creating"] = "After creating",
            ["The task appears on Manage Tasks, where it can be viewed, edited, searched, or deleted."] =
"The task appears on Manage Tasks, where it can be viewed, edited, searched, or deleted.",

            ["Clean separation"] = "Clean separation",
            ["Create stays here. Management stays in the table page."] =
"Create stays here. Management stays in the table page.",

            //Profesor TASKS(details)
            ["Task"] = "Task",

            ["Read-only task details and management context."] =
"Read-only task details and management context.",

            ["Back to Manage Tasks"] = "Back to Manage Tasks",

            ["Details"] = "Details",

            ["Completed"] = "Completed",
            ["Active"] = "Active",

            ["Group"] = "Group",

            ["Unassigned"] = "Unassigned",

            ["Deadline"] = "Deadline",

            ["Submissions"] = "Submissions",

            ["Download task file"] = "Download task file",

            ["Review"] = "Review",

            ["Student submissions"] = "Student submissions",

            ["submissions"] = "submissions",

            ["Open the submissions page to review files and feedback."] =
"Open the submissions page to review files and feedback.",

            ["View Submissions"] = "View Submissions",

            //profesor TASKS(index)
            ["Manage Tasks"] = "Manage Tasks",

            ["Task inventory"] = "Task inventory",

            ["Create tasks here, then use this table for management."] =
"Create tasks here, then use this table for management.",

            ["Search"] = "Search",

            ["Search title or description"] =
"Search title or description",

            ["Status"] = "Status",

            ["All statuses"] = "All statuses",

            ["Active"] = "Active",
            ["Completed"] = "Completed",

            ["Apply"] = "Apply",
            ["Reset"] = "Reset",

            ["No tasks found"] = "No tasks found",

            ["Adjust the filters or create a task from the Tasks section."] =
"Adjust the filters or create a task from the Tasks section.",

            ["Actions"] = "Actions",

            ["Manage"] = "Manage",

            ["Edit"] = "Edit",

            ["Delete"] = "Delete",

            ["Delete task?"] = "Delete task?",

            ["This task and its submissions will be permanently deleted. This action cannot be undone."] =
"This task and its submissions will be permanently deleted. This action cannot be undone.",

            ["Edit Task"] = "Edit Task",

            ["Save"] = "Save",

            ["Cancel"] = "Cancel",

            ["Choose group"] = "Choose group",

            ["Tasks are assigned to students who belong to this group."] =
"Tasks are assigned to students who belong to this group.",

            ["Optional file"] = "Optional file",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Supported files: PDF, DOCX, ZIP, PNG, JPG.",
            ["Unassigned"] = "Unassigned",

            //profesor TASKS(submissions)
            ["Task Submissions"] = "Task Submissions",

            ["Professor"] = "Professor",

            ["Review submitted files, comments, and save feedback for {0}."] =
"Review submitted files, comments, and save feedback for {0}.",

            ["this task"] = "this task",

            ["Back to Tasks"] = "Back to Tasks",

            ["Submissions"] = "Submissions",
            ["Submission"] = "Submission",

            ["Student work"] = "Student work",

            ["Feedback marks a submission as reviewed."] =
"Feedback marks a submission as reviewed.",

            ["No submissions yet"] =
"No submissions yet",

            ["Student submissions for this task will appear here."] =
"Student submissions for this task will appear here.",

            ["Student"] = "Student",

            ["Submitted"] = "Submitted",

            ["File name"] = "File name",

            ["Submitted file"] = "Submitted file",

            ["View File"] = "View File",

            ["Download"] = "Download",

            ["Student note"] = "Student note",

            ["Feedback"] = "Feedback",

            ["Reviewed"] = "Reviewed",

            ["Needs review"] = "Needs review",

            ["Grade"] = "Grade",

            ["Reviewed without written feedback."] =
"Reviewed without written feedback.",

            ["No feedback yet."] =
"No feedback yet.",

            ["Edit feedback"] = "Edit feedback",

            ["Add feedback"] = "Add feedback",

            ["Delete feedback?"] =
"Delete feedback?",

            ["This feedback will be permanently deleted and the submission will return to Submitted status."] =
"This feedback will be permanently deleted and the submission will return to Submitted status.",

            ["Delete feedback"] = "Delete feedback",

            ["Review submission"] =
"Review submission",

            ["Write clear feedback"] =
"Write clear feedback",

            ["Grade optional"] =
"Grade optional",

            ["Save feedback"] =
"Save feedback",

            ["Cancel"] = "Cancel",

            //profesor topics batchstart
            ["Batch Topics"] = "Batch Topics",

            ["Professor"] = "Professor",

            ["Manage the topics inside this batch before publishing them for students."] =
"Manage the topics inside this batch before publishing them for students.",

            ["Back to Batches"] = "Back to Batches",

            ["Created At"] = "Created At",

            ["Status"] = "Status",

            ["Active"] = "Active",
            ["Draft"] = "Draft",

            ["Topics"] = "Topics",

            ["Batch topic list"] = "Batch topic list",

            ["Review the topics in this batch and continue building it over time."] =
"Review the topics in this batch and continue building it over time.",

            ["Add Topics"] = "Add Topics",

            ["No topics in this batch yet"] =
"No topics in this batch yet",

            ["This batch is ready for topic management when you add the next step."] =
"This batch is ready for topic management when you add the next step.",

            ["Search"] = "Search",

            ["Search topic title"] =
"Search topic title",

            ["All statuses"] =
"All statuses",

            ["Available"] = "Available",

            ["Taken"] = "Taken",

            ["Reset"] = "Reset",

            ["Topic title"] = "Topic title",

            ["Selected by"] = "Selected by",

            ["Files"] = "Files",

            ["Actions"] = "Actions",

            ["Edit"] = "Edit",

            ["Delete"] = "Delete",

            ["Delete topic?"] = "Delete topic?",

            ["This topic will be permanently deleted from the batch. This action cannot be undone."] =
"This topic will be permanently deleted from the batch. This action cannot be undone.",

            ["Topic"] = "Topic",

            ["Edit Topic"] = "Edit Topic",

            ["Cancel"] = "Cancel",

            ["Save"] = "Save",

            ["Topic files"] = "Topic files",

            ["No files attached"] =
"No files attached",

            ["Add files to this specific topic when you need supporting material."] =
"Add files to this specific topic when you need supporting material.",

            ["Add file"] = "Add file",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Supported files: PDF, DOCX, ZIP, PNG, JPG.",

            ["Upload file"] = "Upload file",

            ["Close"] = "Close",

            ["One topic"] = "One topic",

            ["Multiple topics"] = "Multiple topics",

            ["Choose topic add mode"] =
"Choose topic add mode",

            ["Enter topic title"] =
"Enter topic title",

            ["Topic file optional"] =
"Topic file optional",

            ["Attach one supporting file to this topic only. Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Attach one supporting file to this topic only. Supported files: PDF, DOCX, ZIP, PNG, JPG.",

            ["Topic titles"] =
"Topic titles",

            ["Write one topic per line"] =
"Write one topic per line",

            ["Each line will be added as a separate topic."] =
"Each line will be added as a separate topic.",

            //profesor topics create
            ["Create Batch"] = "Create Batch",

            ["Professor"] = "Professor",

            ["Create a focused topic batch by adding a title and listing topics one per line."] =
"Create a focused topic batch by adding a title and listing topics one per line.",

            ["Topics"] = "Topics",

            ["Batch details"] =
"Batch details",

            ["Add a batch name and provide each topic on its own line."] =
"Add a batch name and provide each topic on its own line.",

            ["Topic batch title"] =
"Topic batch title",

            ["Write one topic per line."] =
"Write one topic per line.",

            ["Cancel"] = "Cancel",

            ["Preview"] = "Preview",

            ["Review before saving"] =
"Review before saving",

            ["No topics yet"] =
"No topics yet",

            ["No topics to preview"] =
"No topics to preview",

            ["Paste topics to see the list here."] =
"Paste topics to see the list here.",

            //profesor topics index
            ["Manage Topics"] = "Manage Topics",

            ["Topic Batches"] = "Topic Batches",

            ["Your topic inventory"] =
"Your topic inventory",

            ["Create topic lists here, then manage existing batches in the table."] =
"Create topic lists here, then manage existing batches in the table.",

            ["Create Batch"] =
"Create Batch",

            ["Search"] =
"Search",

            ["Search batch or topic title"] =
"Search batch or topic title",

            ["Status"] =
"Status",

            ["All statuses"] =
"All statuses",

            ["Active"] =
"Active",

            ["Draft"] =
"Draft",

            ["Apply"] =
"Apply",

            ["Reset"] =
"Reset",

            ["No topic batches found"] =
"No topic batches found",

            ["Adjust the filters or create a new topic list from the Topics section."] =
"Adjust the filters or create a new topic list from the Topics section.",

            ["Topic batch"] =
"Topic batch",

            ["Professor"] =
"Professor",

            ["Topics"] =
"Topics",

            ["Actions"] =
"Actions",

            ["Created"] =
"Created",

            ["total"] =
"total",

            ["available"] =
"available",

            ["taken"] =
"taken",

            ["Manage"] =
"Manage",

            ["Activate"] =
"Activate",

            ["Edit"] =
"Edit",

            ["Delete"] =
"Delete",

            ["Delete topic batch?"] =
"Delete topic batch?",

            ["This topic batch and its topics will be permanently deleted. This action cannot be undone."] =
"This topic batch and its topics will be permanently deleted. This action cannot be undone.",

            ["Edit Topic Batch"] =
"Edit Topic Batch",

            ["Title"] =
"Title",

            ["Cancel"] =
"Cancel",

            ["Save"] =
"Save",

            ["Activate Batch"] =
"Activate Batch",

            ["Make this batch visible?"] =
"Make this batch visible?",

            ["Students will be able to view this batch and select available topics after activation."] =
"Students will be able to view this batch and select available topics after activation.",

            ["Topic batch title"] =
"Topic batch title",

            ["Write one topic per line. New batches start as Draft and stay hidden from students until activated."] =
"Write one topic per line. New batches start as Draft and stay hidden from students until activated.",

            ["Preview"] =
"Preview",

            ["Review before saving"] =
"Review before saving",

            ["No topics yet"] =
"No topics yet",

            ["No topics to preview"] =
"No topics to preview",

            ["Paste topics into the create modal to see them here."] =
"Paste topics into the create modal to see them here.",

            //settings
            ["Please fix the following issues:"] =
"Please fix the following issues:",

            ["Account"] =
"Account",

            ["Account profile"] =
"Account profile",

            ["Manage the identity shown across your workspace."] =
"Manage the identity shown across your workspace.",

            ["Change profile photo"] =
"Change profile photo",

            ["Change photo"] =
"Change photo",

            ["Full Name"] =
"Full Name",

            ["Enter your full name"] =
"Enter your full name",

            ["Email"] =
"Email",

            ["Email is locked for account security."] =
"Email is locked for account security.",

            ["Role"] =
"Role",

            ["Role is managed by the account type."] =
"Role is managed by the account type.",

            ["Save changes"] =
"Save changes",

            ["Preferences"] =
"Preferences",

            ["Visual mode"] =
"Visual mode",

            ["Switch between a bright productivity setup and a darker, focused look."] =
"Switch between a bright productivity setup and a darker, focused look.",

            ["Light"] =
"Light",

            ["Bright cards and crisp contrast for daytime work."] =
"Bright cards and crisp contrast for daytime work.",

            ["Dark"] =
"Dark",

            ["Deeper surfaces for late sessions and reduced glare."] =
"Deeper surfaces for late sessions and reduced glare.",

            ["Language"] =
"Language",

            ["Interface language"] =
"Interface language",

            ["Choose the language you want to use when language support is enabled."] =
"Choose the language you want to use when language support is enabled.",

            ["shqip"] =
"Albanian",

            ["english"] =
"English",

            ["Save language"] =
"Save language",

            ["Password"] =
"Password",

            ["Change password"] =
"Change password",

            ["Verify your current password before saving a new one."] =
"Verify your current password before saving a new one.",

            ["Current password"] =
"Current password",

            ["New password"] =
"New password",

            ["Confirm password"] =
"Confirm password",

            ["Update password"] =
"Update password",

            ["Danger Zone"] =
"Danger Zone",

            ["Delete account"] =
"Delete account",

            ["Permanent"] =
"Permanent",

            ["This permanently removes your account and clears the current session."] =
"This permanently removes your account and clears the current session.",

            ["Delete account?"] =
"Delete account?",

            ["This account will be permanently deleted. Your access will be removed immediately and you will be signed out."] =
"This account will be permanently deleted. Your access will be removed immediately and you will be signed out.",

            ["Deleting account..."] =
"Deleting account...",

            ["Delete my account"] =
"Delete my account",

            //dashboard
            ["Workspace"] = "Workspace",

            ["Dashboard"] = "Dashboard",

            ["Topics"] = "Topics",

            ["Available Topics"] = "Available Topics",

            ["My Topic"] = "My Topic",

            ["Tasks"] = "Tasks",

            ["Groups"] = "Groups",

            ["Settings"] = "Settings",

            ["Log out"] = "Log out",

            ["Notifications"] = "Notifications",

            ["Loading"] = "Loading",

            ["Loading updates..."] = "Loading updates...",

            ["Professor"] = "Professor",

            ["Student"] = "Student",
            ["Faculty Projects"] = "Faculty Projects",
            ["Review groups after you request to join or after your request is accepted."] =
"Review groups after you request to join or after your request is accepted.",
            ["Search for professors in Groups and send a join request. Requested or accepted groups will appear here."] =
"Search for professors in Groups and send a join request. Requested or accepted groups will appear here.",
            ["Professor not assigned"] = "Professor not assigned",
            ["Open spots"] = "Open spots",
            ["Your request is pending. Topics become active after acceptance."] =
"Your request is pending. Topics become active after acceptance.",
            ["Your group is active. Review your selected topic and available options."] =
"Your group is active. Review your selected topic and available options.",
            ["Download file"] = "Download file",
            ["Request pending"] = "Request pending",
            ["This group is visible because you sent a join request. It will become active when the professor accepts you into the group."] =
"This group is visible because you sent a join request. It will become active when the professor accepts you into the group.",
            ["Already selected"] = "Already selected",
            ["Select Topic"] = "Select Topic",
            ["Filled"] = "Filled",
            ["file"] = "file",
            ["files"] = "files",

            //delete modal
            ["Permanent Delete"] = "Permanent Delete",

            ["Delete item?"] = "Delete item?",

            ["This item will be permanently deleted. This action cannot be undone."] =
"This item will be permanently deleted. This action cannot be undone.",

            ["Cancel"] = "Cancel",

            ["Delete"] = "Delete",

            ["Close"] = "Close",

            //LAYOUT
            ["Features"] = "Features",
            ["How It Works"] = "How It Works",
            ["Benefits"] = "Benefits",
            ["Login"] = "Login",
            ["Register"] = "Register",
            ["Real-time academic collaboration for students and professors, built for clarity, structure, and progress."] =
"Real-time academic collaboration for students and professors, built for clarity, structure, and progress.",
            ["Home"] = "Home",
            ["Privacy"] = "Privacy",

            //foto modal
            ["CollabSpace User"] = "CollabSpace User",

            ["Close photo modal"] = "Close photo modal",

            ["Profile Photo"] = "Profile Photo",

            ["Update your account image"] = "Update your account image",

            ["Choose a new image from your device or reset back to the default profile picture."] =
"Choose a new image from your device or reset back to the default profile picture.",

            ["Choose image"] = "Choose image",

            ["JPG, PNG, GIF, or WEBP up to 5 MB."] =
"JPG, PNG, GIF, or WEBP up to 5 MB.",

            ["Upload Photo"] = "Upload Photo",

            ["Reset to Default"] = "Reset to Default",

            ["Account settings"] = "Account settings",
            ["current profile photo"] = "current profile photo",

            //settings section
            ["Please fix the following issues:"] = "Please fix the following issues:",

            ["Account"] = "Account",
            ["Account profile"] = "Account profile",
            ["Manage the identity shown across your workspace."] =
"Manage the identity shown across your workspace.",

            ["Change profile photo"] = "Change profile photo",
            ["Change photo"] = "Change photo",

            ["Full Name"] = "Full Name",
            ["Enter your full name"] = "Enter your full name",

            ["Email"] = "Email",
            ["Email is locked for account security."] =
"Email is locked for account security.",

            ["Role"] = "Role",
            ["Role is managed by the account type."] =
"Role is managed by the account type.",

            ["Save changes"] = "Save changes",

            ["Preferences"] = "Preferences",

            ["Visual mode"] = "Visual mode",

            ["Switch between a bright productivity setup and a darker, focused look."] =
"Switch between a bright productivity setup and a darker, focused look.",

            ["Light"] = "Light",

            ["Bright cards and crisp contrast for daytime work."] =
"Bright cards and crisp contrast for daytime work.",

            ["Dark"] = "Dark",

            ["Deeper surfaces for late sessions and reduced glare."] =
"Deeper surfaces for late sessions and reduced glare.",

            ["Language"] = "Language",

            ["Interface language"] = "Interface language",

            ["Choose the language you want to use when language support is enabled."] =
"Choose the language you want to use when language support is enabled.",

            ["shqip"] = "Albanian",
            ["english"] = "English",

            ["Save language"] = "Save language",

            ["Password"] = "Password",

            ["Change password"] = "Change password",

            ["Verify your current password before saving a new one."] =
"Verify your current password before saving a new one.",

            ["Current password"] = "Current password",

            ["Show current password"] =
"Show current password",

            ["New password"] = "New password",

            ["Show new password"] =
"Show new password",

            ["Confirm password"] = "Confirm password",

            ["Show password confirmation"] =
"Show password confirmation",

            ["Update password"] = "Update password",

            ["Danger Zone"] = "Danger Zone",

            ["Delete account"] = "Delete account",

            ["Permanent"] = "Permanent",

            ["This permanently removes your account and clears the current session."] =
"This permanently removes your account and clears the current session.",

            ["Delete account?"] =
"Delete account?",

            ["This account will be permanently deleted. Your access will be removed immediately and you will be signed out."] =
"This account will be permanently deleted. Your access will be removed immediately and you will be signed out.",

            ["Deleting account..."] =
"Deleting account...",

            ["Delete my account"] =
"Delete my account",

            ["profile photo"] =
"profile photo",

            //Details
            ["Submitted"] = "Submitted",

            ["Deadline Passed"] = "Deadline Passed",

            ["Active"] = "Active",

            ["submitted"] = "submitted",

            ["deadline-passed"] = "deadline-passed",

            ["active"] = "active",

            ["Reviewed"] = "Reviewed",

            ["Student Task"] = "Student Task",

            ["Review the task details and submit your completed work when it is ready."] =
"Review the task details and submit your completed work when it is ready.",

            ["Back to tasks"] =
"Back to tasks",

            ["Task Details"] =
"Task Details",

            ["Professor"] =
"Professor",

            ["Group"] =
"Group",

            ["Deadline"] =
"Deadline",

            ["Open guidance link"] =
"Open guidance link",

            ["Download task file"] =
"Download task file",

            ["Submission"] =
"Submission",

            ["Your work"] =
"Your work",

            ["File"] =
"File",

            ["Comment"] =
"Comment",

            ["Professor feedback"] =
"Professor feedback",

            ["Grade:"] =
"Grade:",

            ["Deadline passed"] =
"Deadline passed",

            ["Submissions and changes are closed for this task."] =
"Submissions and changes are closed for this task.",

            ["Completed work file"] =
"Completed work file",

            ["Upload improved file"] =
"Upload improved file",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Supported files: PDF, DOCX, ZIP, PNG, JPG.",

            ["Comment optional"] =
"Comment optional",

            ["Add a short note for the professor"] =
"Add a short note for the professor",

            ["Submit Work"] =
"Submit Work",

            ["Update Submission"] =
"Update Submission",

            ["Delete submission?"] =
"Delete submission?",

            ["This submission will be permanently deleted. This action cannot be undone."] =
"This submission will be permanently deleted. This action cannot be undone.",

            ["Delete submission"] =
"Delete submission",


            //student tasks index
            ["Student"] = "Student",

            ["Tasks"] = "Tasks",

            ["Review assigned tasks, deadlines, and any meeting or guidance links shared by professors."] =
"Review assigned tasks, deadlines, and any meeting or guidance links shared by professors.",

            ["No tasks available"] =
"No tasks available",

            ["Group tasks will appear here after your membership is accepted."] =
"Group tasks will appear here after your membership is accepted.",

            ["Group"] =
"Group",

            ["Task"] =
"Task",

            ["Submitted"] =
"Submitted",

            ["Deadline Passed"] =
"Deadline Passed",

            ["Active"] =
"Active",

            ["submitted"] =
"submitted",

            ["deadline-passed"] =
"deadline-passed",

            ["active"] =
"active",

            ["Reviewed"] =
"Reviewed",

            ["Student Task"] =
"Student Task",

            ["Deadline"] =
"Deadline",

            ["Professor"] =
"Professor",

            ["Download task file"] =
"Download task file",

            ["No task file attached"] =
"No task file attached",

            ["Open guidance link"] =
"Open guidance link",

            ["Submit Work"] =
"Submit Work",

            ["Submissions are closed."] =
"Submissions are closed.",

            ["Submitted file"] =
"Submitted file",

            ["Comment"] =
"Comment",

            ["Grade"] =
"Grade",

            ["Submission changes are closed."] =
"Submission changes are closed.",

            ["Update Submission"] =
"Update Submission",

            ["Delete submission?"] =
"Delete submission?",

            ["This submission will be permanently deleted. This action cannot be undone."] =
"This submission will be permanently deleted. This action cannot be undone.",

            ["Delete submission"] =
"Delete submission",

            ["Completed work file"] =
"Completed work file",

            ["Upload improved file"] =
"Upload improved file",

            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Supported files: PDF, DOCX, ZIP, PNG, JPG.",

            ["Add a short note for the professor"] =
"Add a short note for the professor",

            ["Cancel"] =
"Cancel",

            //student tasks submited task
            ["Student"] = "Student",
            ["Tasks"] = "Tasks",
            ["Review assigned tasks, deadlines, and any meeting or guidance links shared by professors."] =
"Review assigned tasks, deadlines, and any meeting or guidance links shared by professors.",
            ["No tasks available"] = "No tasks available",
            ["Group tasks will appear here after your membership is accepted."] =
"Group tasks will appear here after your membership is accepted.",
            ["Group"] = "Group",
            ["Task"] = "Task",
            ["Submitted"] = "Submitted",
            ["Deadline Passed"] = "Deadline Passed",
            ["Active"] = "Active",
            ["Reviewed"] = "Reviewed",
            ["Student Task"] = "Student Task",
            ["Deadline"] = "Deadline",
            ["Professor"] = "Professor",
            ["Download task file"] = "Download task file",
            ["No task file attached"] = "No task file attached",
            ["Open guidance link"] = "Open guidance link",
            ["Submit Work"] = "Submit Work",
            ["Submissions are closed."] = "Submissions are closed.",
            ["Submitted file"] = "Submitted file",
            ["Comment"] = "Comment",
            ["Professor feedback"] = "Professor feedback",
            ["Grade"] = "Grade",
            ["Submission changes are closed."] = "Submission changes are closed.",
            ["Update Submission"] = "Update Submission",
            ["Delete submission?"] = "Delete submission?",
            ["This submission will be permanently deleted. This action cannot be undone."] =
"This submission will be permanently deleted. This action cannot be undone.",
            ["Delete submission"] = "Delete submission",
            ["Completed work file"] = "Completed work file",
            ["Upload improved file"] = "Upload improved file",
            ["Supported files: PDF, DOCX, ZIP, PNG, JPG."] =
"Supported files: PDF, DOCX, ZIP, PNG, JPG.",
            ["Comment optional"] = "Comment optional",
            ["Add a short note for the professor"] = "Add a short note for the professor",
            ["Cancel"] = "Cancel",
            ["Close"] = "Close",

            //student topic index

            //mytopic

            // Sidebar
            ["Dashboard"] = "Dashboard",
            ["Topics"] = "Topics",
            ["Tasks"] = "Tasks",
            ["Groups"] = "Groups",
            ["Notifications"] = "Notifications",
            ["Settings"] = "Settings",
            ["Logout"] = "Logout",
            ["Workspace"] = "Workspace"
        };
    }
}

