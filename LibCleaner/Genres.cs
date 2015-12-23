using System.Collections.Generic;

namespace LibCleaner
{
    public class GenresCategory
    {
        public char Code { get; set; }
        public string Name { get; set; }
        public Genres[] Genres { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Code, Name);
        }

        public GenresCategory(char code, string name, Genres[] genres)
        {
            Code = code;
            Name = name;
            Genres = genres;
        }
    }

    public class Genres
    {
        public char CategoryCode { get; set; }
        public char SelfCode { get; set; }
        public string Code { get; set; }
        public string ShortName { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Code, Name);
        }

        public Genres(char categoryCode, char selfCode, string shortName, string name, bool marked = false)
        {
            CategoryCode = categoryCode;
            SelfCode = selfCode;
            ShortName = shortName;
            Name = name;

            Selected = marked;
            Code = string.Format("{0}{1}", CategoryCode, SelfCode);
        }

        public Genres(string code, string name, bool selected)
        {
            Code = code;
            Name = name;
            Selected = selected;
        }
    }

    public static class GenresListContainer
    {
        #region categories
        public static GenresCategory[] Categories =
        {
            new GenresCategory('1', "Fiction", new[]
            {
                new Genres('1','1',"sf_history","Alternative History"), 
                new Genres('1','2',"sf_action","Fighting fantasy"), 
                new Genres('1','3',"sf_epic","Epic fantasy"), 
                new Genres('1','4',"sf_heroic","Heroic fantasy"), 
                new Genres('1','5',"sf_detective","Detective Fiction"), 
                new Genres('1','6',"sf_cyberpunk","Cyberpunk"), 
                new Genres('1','7',"sf_space","Space Fiction"), 
                new Genres('1','8',"sf_social","Socio-psychological fiction"), 
                new Genres('1','9',"sf_horror","Horror and Mysticism"), 
                new Genres('1','A',"sf_humor","Humorous Fiction"), 
                new Genres('1','B',"sf_fantasy","Fantasy"), 
                new Genres('1','0',"sf","Science Fiction") 
            }),
            new GenresCategory('2', "Mysteries and Thrillers", new[]
            {
                new Genres('2','1',"det_classic","Classic mystery"), 
                new Genres('2','2',"det_police","Police mystery"), 
                new Genres('2','3',"det_action","Action"), 
                new Genres('2','4',"det_irony","Ironic mystery"), 
                new Genres('2','5',"det_history","Historical mystery"), 
                new Genres('2','6',"det_espionage","Spy mystery"), 
                new Genres('2','7',"det_crime","Criminal mystery"), 
                new Genres('2','8',"det_political","Political mystery"), 
                new Genres('2','9',"det_maniac","Maniacs"), 
                new Genres('2','A',"det_hard","Action mystery"), 
                new Genres('2','B',"thriller","Thriller"), 
                new Genres('2','0',"detective","Mystery") 
            }),
            new GenresCategory('3', "Prose", new[]
            {
                new Genres('3','1',"prose_classic","Classical prose"), 
                new Genres('3','2',"prose_history","Historical prose"), 
                new Genres('3','3',"prose_contemporary","Modern prose"), 
                new Genres('3','4',"prose_counter","Counterculture"), 
                new Genres('3','5',"prose_rus_classic","Russian Classical prose"), 
                new Genres('3','6',"prose_su_classics","Soviet Classical prose"), 
                new Genres('3','7',"prose_military","Prose military"), 
                new Genres('3','0',"prose","Prose") 
            }),
            new GenresCategory('4', "Romance", new[]
            {
                new Genres('4','1',"love_contemporary","Modern romance novels", true), 
                new Genres('4','2',"love_history","Historical romance novels", true), 
                new Genres('4','3',"love_detective","Action romance novels", true), 
                new Genres('4','4',"love_short","Short romance novels", true), 
                new Genres('4','5',"love_erotica","Erotic novels", true), 
                new Genres('4','6',"love_sf","Love science fiction", true), 
                new Genres('4','0',"love","Love novels", true) 
            }),
            new GenresCategory('5', "Adventures", new[]
            {
                new Genres('5','1',"adv_western","Western"), 
                new Genres('5','2',"adv_history","Historical adventures"), 
                new Genres('5','3',"adv_indian","Adventures about Indians"), 
                new Genres('5','4',"adv_maritime","Sea adventures"), 
                new Genres('5','5',"adv_geo","Travel and Geography"), 
                new Genres('5','6',"adv_animal","Nature and Animals"), 
                new Genres('5','0',"adventure","Adventures") 
            }),
            new GenresCategory('6', "Children literature", new[]
            {
                new Genres('6','1',"child_tale","Tales"), 
                new Genres('6','2',"child_verse","Children poetry"), 
                new Genres('6','3',"child_prose","Childern prose"), 
                new Genres('6','4',"child_sf","Children fiction"), 
                new Genres('6','5',"child_det","Children action"), 
                new Genres('6','6',"child_adv","Children adventures"), 
                new Genres('6','7',"child_education","Children education"), 
                new Genres('6','0',"children","Children literature") 
            }),
            new GenresCategory('7', "Poetry, dramaturgy", new[]
            {
  		        new Genres('7', '1', "poetry", "Poetry"),
		        new Genres('7', '2', "dramaturgy", "Dramaturgy")
            }),
            new GenresCategory('8', "Antique", new[]
            {
		        new Genres('8', '1', "antique_ant", "Antique Literature"),
		        new Genres('8', '2', "antique_european", "European ancient literature"),
		        new Genres('8', '3', "antique_russian", "Russian ancient literature"),
		        new Genres('8', '4', "antique_east", "Oriental ancient literature"),
		        new Genres('8', '5', "antique_myths", "Myths. Legends. Epic."),
		        new Genres('8', '0', "antique", "Ancient literature")
            }),
            new GenresCategory('9', "Science, Education", new[]
            {
		        new Genres('9', '1', "sci_history", "History"),
		        new Genres('9', '2', "sci_psychology", "Psychology"),
		        new Genres('9', '3', "sci_culture", "Cultural"),
		        new Genres('9', '4', "sci_religion", "Religious"),
		        new Genres('9', '5', "sci_philosophy", "Philosophy"),
		        new Genres('9', '6', "sci_politics", "Politology", true),
		        new Genres('9', '7', "sci_business", "Business literature", true),
		        new Genres('9', '8', "sci_juris", "Law", true),
		        new Genres('9', '9', "sci_linguistic", "Linguistics"),
		        new Genres('9', 'A', "sci_medicine", "Medical"),
		        new Genres('9', 'B', "sci_phys", "Physics"),
		        new Genres('9', 'C', "sci_math", "Mathematics"),
		        new Genres('9', 'D', "sci_chem", "Chemistry"),
		        new Genres('9', 'E', "sci_biology", "Biology"),
		        new Genres('9', 'F', "sci_tech", "Engineering"),
		        new Genres('9', '0', "science", "Scientific literature")
            }),
            new GenresCategory('A', "PC, Internet", new[]
            {
		        new Genres('A', '1', "comp_www", "Internet"),
		        new Genres('A', '2', "comp_programming", "Programming"),
		        new Genres('A', '3', "comp_hard", "Computer hardware"),
		        new Genres('A', '4', "comp_soft", "Software"),
		        new Genres('A', '5', "comp_db", "Databases"),
		        new Genres('A', '6', "comp_osnet", "OS and Network"),
		        new Genres('A', '0', "computers", "Computer literature")
            }),
            new GenresCategory('B', "References", new[]
            {
		        new Genres('B', '1', "ref_encyc", "Encyclopedias", true),
		        new Genres('B', '2', "ref_dict", "Dictionaries", true),
		        new Genres('B', '3', "ref_ref", "Directories", true),
		        new Genres('B', '4', "ref_guide", "Manuals", true),
		        new Genres('B', '0', "reference", "References", true)
            }),
            new GenresCategory('C', "Documentary literature", new[]
            {
		        new Genres('C', '1', "nonf_biography", "Biographies and Memoirs", true),
		        new Genres('C', '2', "nonf_publicism", "Publicism", true),
		        new Genres('C', '3', "nonf_criticism", "Criticism", true),
		        new Genres('C', '4', "design", "Art and Design", true),
		        new Genres('C', '5', "nonfiction", "Documentary literature", true)
            }),
            new GenresCategory('D', "Religion and Spirituality", new[]
            {
		        new Genres('D', '1', "religion_rel", "Religion"),
		        new Genres('D', '2', "religion_esoterics", "Esoterica"),
		        new Genres('D', '3', "religion_self", "Self-improvement"),
		        new Genres('D', '0', "religion", "Religious Literature")
            }),
            new GenresCategory('E', "Humour", new[]
            {
		        new Genres('E', '1', "humor_anecdote", "Jokes"),
		        new Genres('E', '2', "humor_prose", "Humorous prose"),
		        new Genres('E', '3', "humor_verse", "Humorous poetry", true),
		        new Genres('E', '0', "humor", "Humour")
            }),
            new GenresCategory('F', "Home and Family", new[]
            {
		        new Genres('F', '1', "home_cooking", "Cooking", true),
		        new Genres('F', '2', "home_pets", "Pets", true),
		        new Genres('F', '3', "home_crafts", "Hobbies and Crafts"),
		        new Genres('F', '4', "home_entertain", "Entertainment"),
		        new Genres('F', '5', "home_health", "Health"),
		        new Genres('F', '6', "home_garden", "Gardening"),
		        new Genres('F', '7', "home_diy", "Handmade"),
		        new Genres('F', '8', "home_sport", "Sport", true),
		        new Genres('F', '9', "home_sex", "Erotic, sex", true),
		        new Genres('F', '0', "home", "Household"),
		        new Genres('F', 'A', "geo_guides", "Geo guides", true)
            }),
            new GenresCategory('0', "Economy, Business", new[]
            {
		        new Genres('0', '1', "job_hunting", "Job hunting", true),
		        new Genres('0', '2', "management", "Management", true),
		        new Genres('0', '3', "marketing", "Marketing", true),
		        new Genres('0', '4', "banking", "Banking", true),
		        new Genres('0', '5', "stock", "Stock", true),
		        new Genres('0', '6', "accounting", "Accounting", true),
		        new Genres('0', '7', "global_economy", "Global economy", true),
		        new Genres('0', '0', "economics", "Economics", true),
		        new Genres('0', '8', "industries", "Industries", true),
		        new Genres('0', '9', "org_behavior", "Corporate culture", true),
		        new Genres('0', 'A', "personal_finance", "Personal finance", true),
		        new Genres('0', 'B', "real_estate", "Real estate", true),
		        new Genres('0', 'C', "popular_business", "Popular business", true),
		        new Genres('0', 'D', "small_business", "Small business", true),
		        new Genres('0', 'E', "paper_work", "Paper work", true),
		        new Genres('0', 'F', "economics_ref", "Economics reference book", true)
            })
        };
        #endregion categories

        public static List<Genres> GetDefaultItems()
        {
            var result = new List<Genres>();
            foreach (var category in Categories)
                result.AddRange(category.Genres);
            return result;

            return new List<Genres>
            {
                new Genres("E1", "Анекдоты", true),
                new Genres("E2", "Юмористическая проза", true),
                new Genres("E3", "Юмористические стихи", true),
                new Genres("E0", "Юмор", true),
                
                new Genres("44", "Короткие любовные романы", true),
                new Genres("45", "Эротика", true),
                
                new Genres("56", "Природа и животные", true),
                
                new Genres("71", " Поэзия", true),
                new Genres("72", "Драматургия", true),
                
                new Genres("91", "История", true),
                new Genres("92", "Психология", true),
                new Genres("93", "Культурология", true),
                new Genres("94", "Религиоведение", true),
                new Genres("95", "Философия", true),
                new Genres("96", "Политика", true),
                new Genres("97", "Деловая литература", true),
                new Genres("98", "Юриспруденция", true),
                new Genres("99", "Языкознание", true),
                new Genres("9A", "Медицина", true),
                new Genres("9B", "Физика", true),
                new Genres("9C", "Математика", true),
                new Genres("9D", "Химия", true),
                new Genres("9E", "Биология", true),
                new Genres("9F", "Технические науки", true),
                new Genres("90", "Научная литература", true),
                
                new Genres("04", "Банковское дело", true),
                new Genres("00", "Экономика", true),
                new Genres("09", "Корпоративная культура", true),
                new Genres("0C", "О бизнесе популярно", true),
                new Genres("0F", "Справочники по экономике", true),
           
                new Genres("A1", "Интернет", true),
                new Genres("A2", "Программирование", true),
                new Genres("A3", "Компьютерное железо", true),
                new Genres("A4", "Программы", true),
                new Genres("A5", "Базы данных", true),
                new Genres("A6", "ОС и сети", true),
                new Genres("A0", "Компьтерная литература", true),

                new Genres("B1", "Энциклопедии", true),
                new Genres("B2", "Словари", true),
                new Genres("B3", "Справочники", true),
                new Genres("B4", "Руководства", true),
                new Genres("B0", "Справочная литература", true),
                
                new Genres("C1", "Биографии и Мемуары", true),
                new Genres("C2", "Публицистика", true),
                new Genres("C3", "критика", true),
                new Genres("C4", "Искусство и Дизайн", true),
                new Genres("C5", "Документальная литература", true),

                new Genres("D1", "Религия", true),
                new Genres("D2", "эзотерика", true),
                new Genres("D3", "Самосовершенствование", true),
                new Genres("D0", "Религиозная литература", true),
                
                new Genres("F1", "Кулинария", true),
                new Genres("F2", "Домашние животные", true),
                new Genres("F3", "Хобби и ремесла", true),
                new Genres("F4", "Развлечения", true),
                new Genres("F5", "Здоровье", true),
                new Genres("F6", "Сад и огород", true),
                new Genres("F7", "Сделай сам", true),
                new Genres("F8", "Спорт", true),
                new Genres("F9", "Эротика, Секс", true),
                new Genres("F0", "Домоводство", true),
                new Genres("FA", "Путеводители", true),
            };
        }
    }
}
