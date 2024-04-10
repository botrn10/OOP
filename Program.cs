using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Runtime.CompilerServices;

namespace BaseClasses
{
    class Program
    {
        static readonly Warehouse Warehouse = new();

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            while (true) Command(Console.ReadLine());
        }

        static void Command(string? command)
        {
            Console.WriteLine(StringHandle(command));
            switch (StringHandle(command))
            {
                case "thoat": Environment.Exit(0); return;
                case "xoa man hinh": Console.Clear(); return;
                default: Console.WriteLine("Câu lệnh không hợp lệ!"); return;
            }
        }

        static string StringHandle(string s)
        {
            return Warehouse.SimplifyString(s);
        }

        static void AddItem(string s)
        {

        }
    }

    /// <summary>
    /// Kho chứa.
    /// </summary>
    public class Warehouse : List<Good>, IStringHandle
    {
        public delegate bool FilterDelegate(object obj);

        public new void Add(Good newGood)
        {
            foreach (Good good in this)
                if (newGood.Compare(good, "Count"))
                {
                    good.Count += newGood.Count;
                    newGood = good;
                    return;
                }

            newGood.StorageDate = DateTime.Now;
            newGood.Container = this;
            base.Add(newGood);
        }

        public IEnumerable<Good> Search(string idOrName)
        {
            foreach (Good good in this)
                if (SimplifyString(good.Name).Contains(SimplifyString(idOrName)) || SimplifyString(good.Id).Contains(SimplifyString(idOrName)))
                    yield return good;
        }

        public IEnumerable<Good> Search(string idOrName, FilterDelegate comparer)
        {
            foreach (Good good in Filter(comparer))
                if (SimplifyString(good.Name).Contains(SimplifyString(idOrName)) || SimplifyString(good.Id).Contains(SimplifyString(idOrName)))
                    yield return good;
        }

        public string SimplifyString(string str)
        {
            string NormalizedText = str.Normalize(NormalizationForm.FormD);
            Regex regex = new Regex(@"\p{IsCombiningDiacriticalMarks}+");
            return Regex.Replace(regex.Replace(NormalizedText, string.Empty).Normalize(NormalizationForm.FormC).ToLower(), @"\s+", " ").Trim();
        }

        public IEnumerable<Good> Filter(FilterDelegate comperer)
        {
            foreach (Good good in this)
                if (comperer(good)) yield return good;
        }

        public IEnumerable<Good> Filter<T>()
        {
            bool Compare(object obj)
            {
                if (typeof(T) == obj.GetType()) return true;
                else return false;
            }
            return Filter(Compare);
        }
    }

    /// <summary>
    /// So sánh hai đối tượng dựa vào từng cặp thuộc tính, trường dữ liệu.
    /// </summary>
    public class ObjectComparable
    {
        /// <summary>
        /// So sánh hai đối tượng.
        /// </summary>
        /// <param name="ignore">Các thuộc tính, trường dữ liệu muốn bỏ qua việc so sánh.</param>
        public static bool Compare(object obj1, object obj2, params string[] ignore)
        {
            if (obj1.GetType().Name != obj2.GetType().Name) return false;

            if (CompareProperties(obj1, obj2, ignore) && CompareFields(obj1, obj2, ignore)) return true;
            else return false;
        }

        private static bool CompareProperties(object obj1, object obj2, params string[] ignore)
        {
            PropertyInfo[] properties1 = obj1.GetType().GetProperties();
            PropertyInfo[] properties2 = obj2.GetType().GetProperties();

            foreach (PropertyInfo property1 in properties1)
            {
                if (ignore.FirstOrDefault(p => p == property1.Name) != null) continue;

                PropertyInfo? property2 = properties2.FirstOrDefault(p => p.Name == property1.Name);
                if (property2 == null || property1.PropertyType != property2.PropertyType)
                    return false;

                if (!property1.GetValue(obj1).Equals(property2.GetValue(obj2))) return false;
            }
            return true;
        }

        private static bool CompareFields(object obj1, object obj2, params string[] ignore)
        {
            FieldInfo[] fields1 = obj1.GetType().GetFields();
            FieldInfo[] fields2 = obj2.GetType().GetFields();

            foreach (FieldInfo field1 in fields1)
            {
                if (ignore.FirstOrDefault(f => f == field1.Name) != null) continue;

                FieldInfo? field2 = fields2.FirstOrDefault(f => f.Name == field1.Name);
                if (field2 == null || field1.FieldType != field2.FieldType)
                    return false;

                if (!field1.GetValue(obj1).Equals(field2.GetValue(obj2))) return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Interface So sánh.
    /// </summary>
    public interface IComparison
    {
        bool Compare(object obj, params string[] ignore);
        bool TypeCompare(object obj);
    }

    /// <summary>
    /// Interface Tự xác định mã Id.
    /// </summary>
    public interface IAutoIdentified
    {
        Warehouse Container { set; get; }
        string Numbering();
    }

    /// <summary>
    /// Interface Xử lý chuỗi.
    /// </summary>
    public interface IStringHandle
    {
        string SimplifyString(string str);
    }

    /// <summary>
    /// Interface Bảo trì các sản phẩm máy móc, điện tử.
    /// </summary>
    public interface IMaintenance
    {
        void Maintain();
    }

    public interface IExpirationDate
    {
        bool IsExpired();
    }

    /// <summary>
    /// Hàng hoá.
    /// </summary>
    public abstract class Good : IAutoIdentified, IComparison
    {
        protected Good(string id, string name, int count = 0)
        {
            Id = id;
            Name = name;
            Count = count;
            ProductionCompany = string.Empty;
        }

        public string Id;
        public string Name;
        public int Count;
        public string ProductionCompany;
        public DateTime ProductionDate;
        public DateTime StorageDate;
        public string Description = string.Empty;

        public Warehouse Container { set; get; } = new();

        public virtual string Information()
        {
            return
                $"ID: {Id}\n" +
                $"Tên hàng hoá: {Name}\n" +
                $"Số lượng: {Count}\n" +
                $"Công ti sản xuất: {ProductionCompany}\n" +
                $"Ngày sản xuất: {ProductionDate:dd'/'MM'/'yyyy}\n" +
                $"Ngày lưu kho: {StorageDate:dd'/'MM'/'yyyy}";
        }

        public abstract string CheckCondition();

        public string Numbering()
        {
            Good[] goods = Container.Filter(TypeCompare).ToArray();
            if (goods.Length == 0) return GetType().Name.Substring(0, 3).ToUpper() + "0";
            else
            {
                int largest = 0;
                foreach (Good good in goods)
                    if (int.TryParse(good.Id.Substring(3), out int digitPart) && digitPart > largest)
                        largest = digitPart;
                largest++;
                return GetType().Name.Substring(0, 3).ToUpper() + largest.ToString();
            }
        }

        public bool TypeCompare(object obj)
        {
            if (obj.GetType() == GetType()) return true;
            else return false;
        }

        public bool Compare(object obj, params string[] ignore)
        {
            return ObjectComparable.Compare(this, obj, ignore);
        }
    }

    /// <summary>
    /// Thực phẩm.
    /// </summary>
    public abstract class Food : Good, IExpirationDate
    {
        protected Food(string id, string name, int count = 0) : base(id, name, count) { }
        protected Food(string id, string name, int count, DateTime productionDate, DateTime expirationDate) : base(id, name, count)
        {
            ProductionDate = productionDate;
            ExpirationDate = expirationDate;
        }

        public delegate void FoodDelegate(object obj);
        public event FoodDelegate? OutOfDate;

        public DateTime ExpirationDate;

        public abstract bool IsExpired();
    }

    /// <summary>
    /// Thiết bị điện tử.
    /// </summary>
    public abstract class ElectronicDevice : Good, IMaintenance
    {
        protected ElectronicDevice(string id, string name, int count = 0) : base(id, name, count) { }
        public ElectronicDevice(string id, string name, int count, string productionCompany, string modelDesign) : base(id, name, count)
        {
            ProductionCompany = productionCompany;
            ModelDesign = modelDesign;
        }

        public List<Good> Accessories = new();
        public string ModelDesign = string.Empty;

        public abstract void Maintain();
    }

    // Phone.cs
    public sealed class Phone : ElectronicDevice
    {
        public string Color { get; set; }

        public Phone(string id, string name, int count = 0) : base(id, name, count)
        {
        }

        public Phone(string id, string name, int count, string productionCompany, string modelDesign, string color) : base(id, name, count, productionCompany, modelDesign)
        {
            Color = color;
        }

        public override string CheckCondition()
        {
            // Implement the CheckCondition logic for Watch
            return "Phone condition is good.";
        }

        public override void Maintain()
        {
            // Implement the maintenance logic for Phone
        }
    }

    // Laptop.cs
    public sealed class Laptop : ElectronicDevice
    {
        public string Color { get; set; }

        public Laptop(string id, string name, int count = 0) : base(id, name, count)
        {
        }

        public Laptop(string id, string name, int count, string productionCompany, string modelDesign, string color) : base(id, name, count, productionCompany, modelDesign)
        {
            Color = color;
        }

        public override string CheckCondition()
        {
            // Implement the CheckCondition logic for Laptop
            return "Laptop condition is good.";
        }

        public override void Maintain()
        {
            // Implement the maintenance logic for Laptop
        }
    }

    // Watch.cs
    public sealed class Watch : ElectronicDevice
    {
        public string Color { get; set; }

        public Watch(string id, string name, int count = 0) : base(id, name, count)
        {
        }

        public Watch(string id, string name, int count, string productionCompany, string modelDesign, string color) : base(id, name, count, productionCompany, modelDesign)
        {
            Color = color;
        }

        public override string CheckCondition()
        {
            // Implement the CheckCondition logic for Watch
            return "Watch condition is good.";
        }

        public override void Maintain()
        {
            // Implement the maintenance logic for Watch
        }
    }

    /// <summary>
    /// Vật tư y tế.
    /// </summary>
    public abstract class MedicalSupplies : Good, IExpirationDate
    {
        protected MedicalSupplies(string id, string name, int count = 0) : base(id, name, count) { }
        protected MedicalSupplies(string id, string name, int count, DateTime productionDate, DateTime expirationDate) : base(id, name, count)
        {
            ProductionDate = productionDate;
            ExpirationDate = expirationDate;
        }

        public DateTime ExpirationDate;
        public string StorageConditions = string.Empty;

        public abstract bool IsExpired();
    }

    /// <summary>
    /// Phương tiện giao thông.
    /// </summary>
    public abstract class Vehicle : Good ,IMaintenance
    {
        protected Vehicle(string id, string name, int count = 0) : base(id, name, count) { }
        protected Vehicle(string id, string name, int count, string modelDesign) : base(id, name, count)
        {
            ModelDesign = modelDesign;
        }

        public string ModelDesign = string.Empty;

        public abstract void Maintain();
    }

    /// <summary>
    /// Văn phòng phẩm.
    /// </summary>
    public abstract class Stationery : Good
    {
        protected Stationery(string id, string name, int count = 0) : base(id, name, count) { }
    }

    /// <summary>
    /// Hàng may mặc.
    /// </summary>
    public abstract class Apparel : Good
    {
        protected Apparel(string id, string name, int count = 0) : base(id, name, count) { }
    }
}