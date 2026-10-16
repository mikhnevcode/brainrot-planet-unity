using System;
using UnityEngine;
namespace BrainrotPlanet
{
    // Stable original-cast IDs. Never use a paid legacy costume on these rigs.
    public static class OriginalHeroCatalog
    {
        public const int Count=26;
        public const string ContentNamespace="original26-v1";
        public readonly struct Hero
        {
            public readonly string Id,Name,Skill,Help;
            public readonly int Mechanic;
            public readonly Color Main,Accent;
            public Hero(string id,string name,string skill,int mechanic,string main,string accent,string help)
            {Id=id;Name=name;Skill=skill;Mechanic=mechanic;ColorUtility.TryParseHtmlString(main,out Main);ColorUtility.TryParseHtmlString(accent,out Accent);Help=help;}
        }
        public static readonly Hero[] All={
            new Hero("RolloBulk","РОЛЛО БУЛЬК","ШИРОКИЙ ПРОКАТ",0,"#29BDD0","#F78B2D","Ускорение на 3,5 секунды. Вернись к своей краске или перехвати открытый след."),
            new Hero("Pelmolyot","ПЕЛЬМОЛЁТ","ТРИ ПЕЛЬМЕШКА",1,"#F2CC89","#36CBE8","Три мягкие бомбочки летят веером и закрашивают место попадания."),
            new Hero("MorzhoMorozho","МОРЖО-МОРОЖО","ЛЕДЯНОЙ ЧИХ",2,"#79CAD7","#F49CB6","Холодная волна на 4 секунды замедляет соперников поблизости."),
            new Hero("Utyugonos","УТЮГОНОС","ПАРОВОЙ РЫВОК",3,"#249DAB","#F7816E","Паровой разгон на 4 секунды. Удобен для длинной петли и возвращения."),
            new Hero("Makaronika","МАКАРОНИКА","МАКАРОННЫЙ ВИХРЬ",4,"#F3BC43","#F090BC","Закрашивает круг вокруг героя. Применяй возле границы владений."),
            new Hero("ShurshunPaketych","ШУРШУН ПАКЕТЫЧ","БУМАЖНЫЙ ФИНТ",5,"#C78C46","#45C8CB","Короткий разгон и небольшой карман краски для внезапного манёвра."),
            new Hero("BrokkoBoks","БРОККО-БОКС","КОРНЕВОЙ УДАР",6,"#43A652","#F5EDD8","Закрашивает землю и ненадолго замедляет соседних соперников."),
            new Hero("BudiloBegemotilo","БУДИЛО-БЕГЕМОТИЛО","ЗВОНОК ПОБУДКИ",7,"#B88FDC","#FFAC84","Круговая волна краски и замедление соперников рядом."),
            new Hero("Arbuzub","АРБУЗУБ","СЕМЕЧКОВЫЙ ЗАЛП",8,"#4D9D34","#F78691","Цветной залп покрывает землю и замедляет близких соперников."),
            new Hero("FenikFuFu","ФЕНИК ФУ-ФУ","ВЕТЕР В ШАРФЕ",9,"#269DB4","#F58F43","Ускоряет героя и оставляет небольшой всплеск краски."),
            new Hero("ZheleZhmyak","ЖЕЛЕ-ЖМЯК","ЖЕЛЕЙНЫЙ ПРЫЖОК",10,"#7CE85B","#E54BD0","Подбрасывает героя. При приземлении появляется круг краски."),
            new Hero("CherniloChudilo","ЧЕРНИЛО ЧУДИЛО","ЧЕТЫРЕ КИСТИ",11,"#8134BA","#F4B841","Три последовательных мазка расширяют владения в выбранном направлении."),
            new Hero("Bublikopotam","БУБЛИКОПОТАМ","БУБЛИКОВЫЙ ПЛЮХ",12,"#AF79CC","#FFBB4F","Короткий круговой хлопок краски вокруг героя."),
            new Hero("Chaylitka","ЧАЙЛИТКА","ЧАЙНАЯ ТЯГА",3,"#F1828A","#8BE0BB","Паровой разгон на 4 секунды. Удобен для длинной петли и возвращения."),
            new Hero("Kassetych","КАССЕТЫЧ","ЛЕНТОЧНЫЙ РИТМ",7,"#A36BC6","#FFB348","Круговая волна краски и замедление соперников рядом."),
            new Hero("Lampochmel","ЛАМПОЧМЕЛЬ","СВЕТЯЩИЙСЯ СЛЕД",9,"#F5BD37","#FFF2A2","Ускоряет героя и оставляет небольшой всплеск краски."),
            new Hero("Noskoraket","НОСКОРАКЕТ","НОСОЧНЫЙ РАЗГОН",0,"#E47B73","#79C9CD","Ускорение на 3,5 секунды. Вернись к своей краске или перехвати открытый след."),
            new Hero("KhrustoBron","ХРУСТО-БРОНЬ","ХРУСТЯЩИЙ ПРОКАТ",5,"#E1A056","#D4B0DF","Короткий разгон и небольшой карман краски для внезапного манёвра."),
            new Hero("Magnitofyr","МАГНИТОФЫР","МАГНИТНЫЙ ТОЛЧОК",6,"#A566C8","#3CC8DE","Закрашивает землю и ненадолго замедляет соседних соперников."),
            new Hero("Garmoshmyak","ГАРМОШМЯК","РИТМИЧНЫЕ КОЛЬЦА",7,"#D44649","#F9DDAA","Круговая волна краски и замедление соперников рядом."),
            new Hero("Puzyryonysh","ПУЗЫРЁНЫШ","РАДУЖНЫЙ ХЛОПОК",4,"#76D8E3","#F5A3D8","Закрашивает круг вокруг героя. Применяй возле границы владений."),
            new Hero("Klubkomot","КЛУБКОМОТ","ВЯЗАНАЯ ПЕТЛЯ",6,"#B54EB7","#DEB8F2","Закрашивает землю и ненадолго замедляет соседних соперников."),
            new Hero("Lunoplyukh","ЛУНОПЛЮХ","ЛУННОЕ ПАДЕНИЕ",10,"#F0D375","#A6AFF0","Подбрасывает героя. При приземлении появляется круг краски."),
            new Hero("Kaktusifon","КАКТУСИФОН","ЦВЕТОЧНЫЙ ГУДОК",8,"#4FA882","#E8B440","Цветной залп покрывает землю и замедляет близких соперников."),
            new Hero("Pylekhvost","ПЫЛЕХВОСТ","ПЫЛЕСОСНЫЙ ВЫДОХ",11,"#E29348","#4BCBE1","Три последовательных мазка расширяют владения в выбранном направлении."),
            new Hero("PopkornPykh","ПОПКОРН-ПЫХ","ПОПКОРНОВЫЙ ХЛОПОК",12,"#EAC64C","#B2DA79","Короткий круговой хлопок краски вокруг героя."),
        };
        public static bool Valid(int hero)=>hero>=0&&hero<Count;
        public static Hero Get(int hero)
        {if(!Valid(hero))throw new ArgumentOutOfRangeException(nameof(hero));return All[hero];}
        public static int Mechanic(int hero)=>Get(hero).Mechanic;
        public static string Resource(int hero)=>hero==0?"OriginalCastDetail/RolloBulk_v07":"OriginalCast/"+Get(hero).Id+"_v01";
        public static string[] Names()
        {var values=new string[Count];for(int i=0;i<Count;i++)values[i]=All[i].Name;return values;}
        public static string[] Skills()
        {var values=new string[Count];for(int i=0;i<Count;i++)values[i]=All[i].Skill;return values;}
        public static string[] Helps()
        {var values=new string[Count];for(int i=0;i<Count;i++)values[i]=All[i].Help;return values;}
        public static Color[] Colors(bool accents)
        {var values=new Color[Count];for(int i=0;i<Count;i++)values[i]=accents?All[i].Accent:All[i].Main;return values;}
    }
}
