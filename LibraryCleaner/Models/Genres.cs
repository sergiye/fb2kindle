using System.Collections.Generic;

namespace LibraryCleaner
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

        public static GenresCategory[] CategoriesEnglish =
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

        public static GenresCategory[] Categories =
        {
            new GenresCategory('1', "Фантастика", new[]
            {
                new Genres('1','1',"sf_history","Альтернативная история, попаданцы"), 
                new Genres('1','2',"sf_action","Боевая фантастика"), 
                new Genres('1','3',"sf_epic","Эпическая фантастика"), 
                new Genres('1','4',"sf_heroic","Героическая фантастика"), 
                new Genres('1','5',"sf_detective","Детективная фантастика"), 
                new Genres('1','6',"sf_cyberpunk","Киберпанк"), 
                new Genres('1','7',"sf_space","Космическая фантастика"), 
                new Genres('1','8',"sf_social","Социально-психологическая фантастика"), 
                new Genres('1','9',"sf_horror","Ужасы"), 
                new Genres('1','A',"sf_humor","Юмористическая фантастика"), 
                new Genres('1','B',"sf_fantasy","Фэнтези"), 
                new Genres('1','0',"sf","Научная Фантастика") 
            }),
            new GenresCategory('2', "Детективы и Триллеры", new[]
            {
                new Genres('2','1',"det_classic","Классический детектив"), 
                new Genres('2','2',"det_police","Полицейский детектив"), 
                new Genres('2','3',"det_action","Боевик"), 
                new Genres('2','4',"det_irony","Иронический детектив, дамский детективный роман"), 
                new Genres('2','5',"det_history","Исторический детектив"), 
                new Genres('2','6',"det_espionage","Шпионский детектив"), 
                new Genres('2','7',"det_crime","Криминальный детектив"), 
                new Genres('2','8',"det_political","Политический детектив"), 
                new Genres('2','9',"det_maniac","Про маньяков"), 
                new Genres('2','A',"det_hard","Крутой детектив"), 
                new Genres('2','B',"thriller","Триллер"), 
                new Genres('2','0',"detective","Детективы") 
            }),
            new GenresCategory('3', "Проза", new[]
            {
                new Genres('3','1',"prose_classic","Классическая проза"), 
                new Genres('3','2',"prose_history","Историческая проза"), 
                new Genres('3','3',"prose_contemporary","Современная русская и зарубежная проза"), 
                new Genres('3','4',"prose_counter","Контркультура"), 
                new Genres('3','5',"prose_rus_classic","Русская классическая проза"), 
                new Genres('3','6',"prose_su_classics","Советская классическая проза"), 
                new Genres('3','7',"prose_military","Военная проза"), 
                new Genres('3','0',"prose","Проза") 
            }),
            new GenresCategory('4', "Любовные романы", new[]
            {
                new Genres('4','1',"love_contemporary","Современные любовные романы"), 
                new Genres('4','2',"love_history","Исторические любовные романы"), 
                new Genres('4','3',"love_detective","Остросюжетные любовные романы"), 
                new Genres('4','4',"love_short","Короткие любовные романы"), 
                new Genres('4','5',"love_erotica","Эротическая литература"), 
                new Genres('4','6',"love_sf","Любовное фэнтези"), 
                new Genres('4','0',"love","Любовные романы") 
            }),
            new GenresCategory('5', "Приключения", new[]
            {
                new Genres('5','1',"adv_western","Вестерн"), 
                new Genres('5','2',"adv_history","Исторические приключения"), 
                new Genres('5','3',"adv_indian","Вестерн, про индейцев"), 
                new Genres('5','4',"adv_maritime","Морские приключения"), 
                new Genres('5','5',"adv_geo","Путешествия и география"), 
                new Genres('5','6',"adv_animal","Природа и животные"), 
                new Genres('5','0',"adventure","Приключения") 
            }),
            new GenresCategory('6', "Литература для детей", new[]
            {
                new Genres('6','1',"child_tale","Сказки народов мира"), 
                new Genres('6','2',"child_verse","Стихи для детей"), 
                new Genres('6','3',"child_prose","Стихи для детей"), 
                new Genres('6','4',"child_sf","Фантастика для детей"), 
                new Genres('6','5',"child_det","Детская остросюжетная литература"), 
                new Genres('6','6',"child_adv","Приключения для детей и подростков"), 
                new Genres('6','7',"child_education","Детская образовательная литература"), 
                new Genres('6','0',"children","Детская литература") 
            }),
            new GenresCategory('7', "Поэзия, Драматургия", new[]
            {
  		        new Genres('7', '1', "Поэзия", "Поэзия"),
		        new Genres('7', '2', "Драматургия", "Драматургия")
            }),
            new GenresCategory('8', "Старинное", new[]
            {
		        new Genres('8', '1', "antique_ant", "Античная литература"),
		        new Genres('8', '2', "antique_european", "Европейская старинная литература"),
		        new Genres('8', '3', "antique_russian", "Древнерусская литература"),
		        new Genres('8', '4', "antique_east", "Древневосточная литература"),
		        new Genres('8', '5', "antique_myths", "Мифы. Легенды. Эпос"),
		        new Genres('8', '0', "antique", "Старинное")
            }),
            new GenresCategory('9', "Наука, Образование", new[]
            {
		        new Genres('9', '1', "sci_history", "История"),
		        new Genres('9', '2', "sci_psychology", "Психология и психотерапия"),
		        new Genres('9', '3', "sci_culture", "Культурология"),
		        new Genres('9', '4', "sci_religion", "Религиоведение"),
		        new Genres('9', '5', "sci_philosophy", "Философия"),
		        new Genres('9', '6', "sci_politics", "Политика"),
		        new Genres('9', '7', "sci_business", "Деловая литература"),
		        new Genres('9', '8', "sci_juris", "Юриспруденция"),
		        new Genres('9', '9', "sci_linguistic", "Языкознание"),
		        new Genres('9', 'A', "sci_medicine", "Медицина"),
		        new Genres('9', 'B', "sci_phys", "Физика"),
		        new Genres('9', 'C', "sci_math", "Математика"),
		        new Genres('9', 'D', "sci_chem", "Химия"),
		        new Genres('9', 'E', "sci_biology", "Биология, биофизика, биохимия"),
		        new Genres('9', 'F', "sci_tech", "Технические науки"),
		        new Genres('9', '0', "science", "Научная литература")
            }),
            new GenresCategory('A', "Компьютеры и Интернет", new[]
            {
		        new Genres('A', '1', "comp_www", "ОС и Сети, интернет"),
		        new Genres('A', '2', "comp_programming", "Программирование"),
		        new Genres('A', '3', "comp_hard", "Компьютерное 'железо' (аппаратное обеспечение), цифровая обработка сигналов"),
		        new Genres('A', '4', "comp_soft", "Компьютерные программы"),
		        new Genres('A', '5', "comp_db", "Базы данных"),
		        new Genres('A', '6', "comp_osnet", "Операционные системы и сети"),
		        new Genres('A', '0', "computers", "Компьютерная литература")
            }),
            new GenresCategory('B', "Справочная литература", new[]
            {
		        new Genres('B', '1', "ref_encyc", "Энциклопедии"),
		        new Genres('B', '2', "ref_dict", "Словари"),
		        new Genres('B', '3', "ref_ref", "Справочники"),
		        new Genres('B', '4', "ref_guide", "Руководства"),
		        new Genres('B', '0', "reference", "Справочная литература")
            }),
            new GenresCategory('C', "Документальная литература", new[]
            {
		        new Genres('C', '1', "nonf_biography", "Биографии и Мемуары"),
		        new Genres('C', '2', "nonf_publicism", "Публицистика"),
		        new Genres('C', '3', "nonf_criticism", "Критика"),
		        new Genres('C', '4', "design", "Искусство и Дизайн"),
		        new Genres('C', '5', "nonfiction", "Документальная литература")
            }),
            new GenresCategory('D', "Религия, духовность, эзотерика", new[]
            {
		        new Genres('D', '1', "religion_rel", "Религия"),
		        new Genres('D', '2', "religion_esoterics", "Эзотерика, эзотерическая литература"),
		        new Genres('D', '3', "religion_self", "Самосовершенствование"),
		        new Genres('D', '0', "religion", "Религия, религиозная литература")
            }),
            new GenresCategory('E', "Юмор", new[]
            {
		        new Genres('E', '1', "humor_anecdote", "Анекдоты"),
		        new Genres('E', '2', "humor_prose", "Юмористическая проза"),
		        new Genres('E', '3', "humor_verse", "Юмористические стихи"),
		        new Genres('E', '0', "humor", "Юмор")
            }),
            new GenresCategory('F', "Дом и семья", new[]
            {
		        new Genres('F', '1', "home_cooking", "Кулинария"),
		        new Genres('F', '2', "home_pets", "Домашние животные"),
		        new Genres('F', '3', "home_crafts", "Хобби и ремесла"),
		        new Genres('F', '4', "home_entertain", "Развлечения"),
		        new Genres('F', '5', "home_health", "Здоровье"),
		        new Genres('F', '6', "home_garden", "Сад и огород"),
		        new Genres('F', '7', "home_diy", "Сделай сам"),
		        new Genres('F', '8', "home_sport", "Боевые искусства, спорт"),
		        new Genres('F', '9', "home_sex", "Семейные отношения, секс"),
		        new Genres('F', '0', "home", "Домоводство"),
		        new Genres('F', 'A', "geo_guides", "Путеводители, карты, атласы")
            }),
            new GenresCategory('0', "Деловая литература", new[]
            {
		        new Genres('0', '1', "job_hunting", "Трудоустройство"),
		        new Genres('0', '2', "management", "Менеджмент"),
		        new Genres('0', '3', "marketing", "Маркетинг"),
		        new Genres('0', '4', "banking", "Банковское дело"),
		        new Genres('0', '5', "stock", "Рынок"),
		        new Genres('0', '6', "accounting", "Учет"),
		        new Genres('0', '7', "global_economy", "Глобальная экономика"),
		        new Genres('0', '0', "economics", "Экономика"),
		        new Genres('0', '8', "industries", "Индустрия"),
		        new Genres('0', '9', "org_behavior", "Корпоративная культура"),
		        new Genres('0', 'A', "personal_finance", "Личные финансы"),
		        new Genres('0', 'B', "real_estate", "Недвижимость"),
		        new Genres('0', 'C', "popular_business", "Популярный бизнес"),
		        new Genres('0', 'D', "small_business", "Малый бизнес"),
		        new Genres('0', 'E', "paper_work", "Бумажная работа"),
		        new Genres('0', 'F', "economics_ref", "Деловая литература")
            })
        };
        
        #endregion categories

        public static List<Genres> GetDefaultItems()
        {
            var result = new List<Genres>();
            foreach (var category in Categories)
                result.AddRange(category.Genres);
            return result;
        }
    }
}
