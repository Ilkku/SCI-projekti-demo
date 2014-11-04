using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Error
{
    public class Storage
    {
        public List<BoundingBox> Obstacles; // octree/bsp tree obstacles tms
        public BoundingBox BoundingBox;
        public Map Map;
        public AStar PathFinder;
        public Point PackingLocation_AStar;

        Dictionary<int, Product> _products;

        public Storage(int count)
        {
            Obstacles = new List<BoundingBox>(0);
            BoundingBox = new BoundingBox(Vector3.Zero, Vector3.Zero);
            _products = new Dictionary<int, Product>(count);
        }
        // call this after adding obstacles and products
        public void CreateMap(float resolution_in_metres)
        {
            Map = new Map(BoundingBox, resolution_in_metres);
            for (int x = 0; x < Map.SizeX; x++)
            {
                for (int y = 0; y < Map.SizeY; y++)
                {
                    MapNode mapNode = new MapNode();
                    mapNode.IsTraversable = IsTraversable(Map.InternalToPhysicalCoordinates(new Point(x,y)));
                    Map[x, y] = mapNode;
                }
            }
            PathFinder = new AStar(Map);
        }
        public bool IsTraversable(Vector3 position)
        {
            BoundingBox b = new BoundingBox(position - new Vector3(0.5f, 0.5f, 0f), position + new Vector3(0.5f, 0.5f, 2f));
            if (!BoundingBox.Intersects(b)) return false;

            foreach (var obstacle in Obstacles)
            {
                if (obstacle.Intersects(b)) return false;
            }
            foreach (var product in _products.Values)
            {
                if (product.BoundingBox.Intersects(b)) return false;
            }
            return true;
        }
        public void Add(BoundingBox obstacle)
        {
            BoundingBox = BoundingBox.CreateMerged(BoundingBox, obstacle);
            Obstacles.Add(obstacle);
        }

        // TODO
        #region database access
        // returns deep copy of the product, ie. modifying the return value doesn't modify value in database
        public Product GetProduct(int key)
        {
            return _products[key];
        }
        // using this function should be the only way to modify database
        public void ModifyProduct(int key, Product value)
        {
            _products[key] = value;
        }
        // except this
        public void AddProduct(Product value)
        {
            int key = App.Instance.GetUniqueKey();
            _products.Add(key, value);
            BoundingBox = BoundingBox.CreateMerged(BoundingBox, value.BoundingBox);
        }
        //todo remove=siirto johonkin arkistoon?
        #endregion

        public List<int> GetByProductCode(string code)
        {
            return (from x in _products where x.Value.Code == code select x.Key).ToList();
        }
        public void Collect(int key, int amount)
        {
            var item = GetProduct(key);
            item.Amount -= amount;
            //item.CollectionTimes.Add(DateTime.Now); --> dictionary<productKey, List<DateTime>> collectiontimes
            item.ModifiedDate = DateTime.Now;

            if (item.Amount <= 0)
            {
                // TODO
            }
            ModifyProduct(key, item);
        }
        public int FindNearestToCollect(string productCode, int amount, Point location)
        {
            var keys = GetByProductCode(productCode);
            keys = (from key in keys where GetProduct(key).Amount >= amount select key).ToList();

            // TODO
            //if(items.Count == 0) tuotetta ei varastossa

            // find nearest product
            int minIndex = 0;
            float minTime = float.MaxValue;
            for (int i = 0; i < keys.Count; i++)
            {
                Point collectionPoint = Map.FindCollectingPoint(GetProduct(keys[i]).BoundingBox);
                float time;
                PathFinder.FindPath(location, collectionPoint, out time);
                if (time < minTime)
                {
                    minTime = time;
                    minIndex = i;
                }
            }
            return keys[minIndex];
        }
        public List<int> SearchText(string txt)
        {
            var keys = from p in _products where p.Value.Code == txt select p.Key;
            keys = keys.Union(from p in _products where p.Value.Description == txt select p.Key);
            keys = keys.Union(from p in _products where p.Value.PalletCode == txt select p.Key);
            keys = keys.Union(from p in _products where p.Value.ShelfCode == txt select p.Key);
            // remove duplicates
            return keys.Distinct().ToList();
        }
        public List<int> SearchPartialText(string txt)
        {
            // tulokset vois j�rjest�� osuvuuden mukaan
            var keys = from p in _products where Utils.AreSimilar(txt, p.Value.Code) select p.Key;
            keys = keys.Union(from p in _products where Utils.AreSimilar(txt, p.Value.Description) select p.Key);
            keys = keys.Union(from p in _products where Utils.AreSimilar(txt, p.Value.PalletCode) select p.Key);
            keys = keys.Union(from p in _products where Utils.AreSimilar(txt, p.Value.ShelfCode) select p.Key);
            // remove duplicates
            return keys.Distinct().ToList();
        }
    }

    // saapuu lavallinen tavaraa -> new DataBaseEntry()
    public struct Product // product ei viel�k��n hyv� nimi --> struct
    {
        public string Code;// asdfsadfsf26565ddsa
        public string Description;//"ruuvi sinkitty 5x70"
        public string PalletCode;// 1005, E21B3 == lavapaikka
        public string ShelfCode;// 1005/6? E21B3
        public int Amount;// num_packets = amount/PacketSize
        public int PackageSize;
        public DateTime ProductionDate; // when product was manufactured
        public DateTime InsertionDate; // milloin saapunut varastolle / laitettu hyllyyn. Vanhimmat ensin asiakkaalle?
        public DateTime ModifiedDate; // when anything in this DataBaseEntry has (physically) changed
        //n�ist� saa nopeasti ja helposti tehty� vaikka 3d kuvan...
        public BoundingBox BoundingBox;//fyysinen sijainti, xmin ymin zmin xmax ymax zmax. z korkeus, 1.kerroksen lattia z=0
        public string ExtraNotes;
    }
}
