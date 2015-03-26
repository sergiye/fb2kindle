using System.Collections.Generic;

namespace LibCleaner
{
    public class Genres
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; }

        public override string ToString()
        {
            return string.Format("{0} - {1}", Code, Name);
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
        public static List<Genres> GetDefaultItems()
        {
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
