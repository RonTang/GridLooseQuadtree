using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace RonTang
{
    public class GridQuadTree<T>
    {

        private int nodeMaxLevel;

        public int NodeMaxLevel { get { return nodeMaxLevel; } }

        private Rect worldRect;

        public Rect WorldRect { get { return worldRect; } }

        private float objMinLength;

        private float worldLength;

        private GridQuadTreeNode<T> rootNode;

        public readonly float looseFactor = 2;

        private GridQuadTreeNode<T>[][] nodes;

        public GridQuadTree(Vector2 worldCenter, float worldLengthVal, float objMinLengthVal)
	{
        	worldRect = CreateRect(worldCenter, new Vector2(worldLengthVal, worldLengthVal));
		objMinLength = objMinLengthVal;
		worldLength = worldLengthVal;
		Init ();
	}

	public GridQuadTree(Rect worldRectVal,float objMinLengthVal)
	{

		float maxSide = worldRectVal.width >= worldRectVal.height ? worldRectVal.width: worldRectVal.height;
		worldRect = CreateRect(worldRectVal.center, new Vector2(maxSide, maxSide));
		objMinLength = objMinLengthVal;
		worldLength = maxSide;
		Init ();

	}

	public void GetItems(Rect resultRect, ref List<GridQuadTreeItem<T>> itemsList)
	{
		if ( itemsList!= null)
		{
			GetItems(rootNode, resultRect, ref itemsList);
		}
	}

	private void GetItems(GridQuadTreeNode<T> parentNode, Rect resultRect, ref List<GridQuadTreeItem<T>> itemsList)
	{
		parentNode.GetItems (resultRect, ref itemsList);

		if (parentNode.IsLeaf || parentNode.ChildrenItemCount == 0)
			return;

		var nodeTopLeft = nodes[parentNode.NodeLevel + 1][parentNode.TopLeftIndex];
		if (nodeTopLeft.looseRect.Overlaps(resultRect))
			GetItems(nodeTopLeft, resultRect, ref itemsList);

		var nodeTopRight = nodes[parentNode.NodeLevel + 1][parentNode.TopRightIndex];
		if (nodeTopRight.looseRect.Overlaps(resultRect))
			GetItems(nodeTopRight, resultRect, ref itemsList);

		var nodeBottomLeft = nodes[parentNode.NodeLevel + 1][parentNode.BottomLeftIndex];
		if (nodeBottomLeft.looseRect.Overlaps(resultRect))
			GetItems(nodeBottomLeft,resultRect,ref itemsList);

		var nodeBottomRight = nodes[parentNode.NodeLevel + 1][parentNode.BottomRightIndex];
		if (nodeBottomRight.looseRect.Overlaps(resultRect))
			GetItems(nodeBottomRight, resultRect, ref itemsList);

	}

	public bool Insert(GridQuadTreeItem<T> item)
	{
		float maxSide = item.ItemRect.width >= item.ItemRect.height ? item.ItemRect.width : item.ItemRect.height;
		if (item.ItemRect.height > worldLength || item.ItemRect.width > worldLength) 
		{
			Debug.LogError ("Item rect too big, can not insert it");
			return false;
		}
		int level = Mathf.FloorToInt (Mathf.Log (worldLength / maxSide, 2f));
		if (level > nodeMaxLevel) 
		{
			level = nodeMaxLevel;
			Debug.LogWarning ("Maybe this item is too small, but we can add it to max level.");
		}
		Vector2 itemLocalPos = item.Position - worldRect.min;
		int sideNodeCount = (int)Mathf.Pow(2, level);
		float cellLength = worldLength / sideNodeCount;
		int column = (int)(itemLocalPos.x / cellLength);
		int raw = (int)(itemLocalPos.y / cellLength);
		int index = raw * sideNodeCount + column;

		if (nodes[level][index].nodeRect.Contains(item.Position))
		{
			var nodeToAdd = nodes[level][index];
			if (!nodeToAdd.IsLeaf)
			{
				var leftTop = nodes[nodeToAdd.NodeLevel + 1][nodeToAdd.TopLeftIndex];
				var rightTop = nodes[nodeToAdd.NodeLevel + 1][nodeToAdd.TopRightIndex];
				var leftBottom = nodes[nodeToAdd.NodeLevel + 1][nodeToAdd.BottomLeftIndex]; 
				var rightBottom = nodes[nodeToAdd.NodeLevel + 1][nodeToAdd.BottomRightIndex];
				if (leftTop.looseRect.Contains(item.ItemRect))
					nodeToAdd = leftTop;
				else if (rightTop.looseRect.Contains(item.ItemRect))
					nodeToAdd = rightTop;
				else if (leftBottom.looseRect.Contains(item.ItemRect))
					nodeToAdd = leftBottom;
				else if (rightBottom.looseRect.Contains(item.ItemRect))
					nodeToAdd = rightBottom;

			}
			nodeToAdd.Insert(item);
			ChangeAllParentChildrenItemCount(nodeToAdd, 1);
			Debug.Log("insert.....");
			return true;
		}

		return false;
	}

	private void ChangeAllParentChildrenItemCount(GridQuadTreeNode<T> node, int deltaVal)
	{
		int pIndex = node.ParentIndex;
		int pLevel = node.NodeLevel - 1;
		for ( ; pLevel >= 0; pLevel--)
		{
			 nodes[pLevel][pIndex].ChildrenItemCount += deltaVal;
			 pIndex = nodes[pLevel][pIndex].ParentIndex;
		}
	}

	private void ItemMove(GridQuadTreeItem<T> item, GridQuadTreeNode<T> node)
	{
		ChangeAllParentChildrenItemCount(node, -1);
		Insert (item);
	}


	private void Init()
	{
		nodeMaxLevel = Mathf.FloorToInt(Mathf.Log(worldLength / objMinLength, 2f) + 1);
		InitGrid();
		BulidTree();
		rootNode = nodes[0][0];
	}

        private void InitGrid()
        {
            nodes = new GridQuadTreeNode<T>[nodeMaxLevel + 1][];
            for (int i = 0; i <= nodeMaxLevel; i++)
            {
                nodes[i] = new GridQuadTreeNode<T>[(int)Mathf.Pow(4,i)];
            }
        }

        private Rect CreateRect(Vector2 center, Vector2 size)
        {
            Rect rect = Rect.zero;
            rect.size = size;
            rect.center = center;
            return rect;
        }

        private void BulidTree()
        {
            for (int i = 0; i <= nodeMaxLevel; i++)
            {
                int sideNodeCount  = (int)Mathf.Pow(2,i);
                float cellLength = worldLength / sideNodeCount;
               
                for (int r = 0; r < sideNodeCount; r++)
                {
                    for (int c = 0; c < sideNodeCount; c++)
                    {
                        Rect nodeRect = new Rect(worldRect.min.x + c * cellLength, worldRect.min.y + r * cellLength, cellLength, cellLength);
                        nodes[i][r * sideNodeCount + c] = new GridQuadTreeNode<T>(this,nodeRect, i,i==nodeMaxLevel,ItemMove);
                    }
                }

            }

        }
		
    }

    public class GridQuadTreeNode<T>
    {
        
        public Rect nodeRect;
        public Rect looseRect;

        public bool IsLeaf { get; private set; }

        public int ParentIndex
        {
            get;
            private set;
        }

        public int TopLeftIndex
        {
            get;private set;
        }

        public int TopRightIndex
        {
            get;private set;
        }

        public int BottomLeftIndex
        {
            get;private set;
        }

        public int BottomRightIndex
        {
            get;private set;
        }

        public int NodeLevel
        {
            get;private set;
        }

        public int ChildrenItemCount
        {
            get;set;
        }

        private event Action<GridQuadTreeItem<T>, GridQuadTreeNode<T>> Move;
        private GridQuadTree<T> tree;
        private List<GridQuadTreeItem<T>> items;

        private void Init()
        {
            float nodeLength = nodeRect.width;
            if (!IsLeaf)
            {
                float quater = nodeLength / 4f;
                float childeLength = nodeLength / 2f;
                int childSideNodeCount = (int)Mathf.Sqrt(Mathf.Pow(4, NodeLevel + 1));
                Vector2 topLeftCenter = nodeRect.center + new Vector2(-quater, -quater);
                topLeftCenter = topLeftCenter - tree.WorldRect.min;
                int topLeftRow = Mathf.FloorToInt(topLeftCenter.y / childeLength);
                int topLeftColumn = Mathf.FloorToInt(topLeftCenter.x / childeLength);
                TopLeftIndex = topLeftRow * childSideNodeCount + topLeftColumn;

                Vector2 topRightCenter = nodeRect.center + new Vector2(quater, -quater);
                topRightCenter = topRightCenter - tree.WorldRect.min;
                int topRightRow = Mathf.FloorToInt(topRightCenter.y / childeLength);
                int topRightColumn = Mathf.FloorToInt(topRightCenter.x / childeLength);
                TopRightIndex = topRightRow * childSideNodeCount + topRightColumn;

                Vector2 bottomLeftCenter = nodeRect.center + new Vector2(-quater, quater);
                bottomLeftCenter = bottomLeftCenter - tree.WorldRect.min;
                int bottomLeftRow = Mathf.FloorToInt(bottomLeftCenter.y / childeLength);
                int bottomLeftColumn = Mathf.FloorToInt(bottomLeftCenter.x / childeLength);
                BottomLeftIndex = bottomLeftRow * childSideNodeCount + bottomLeftColumn;

                Vector2 bottomRightCenter = nodeRect.center + new Vector2(quater, quater);
                bottomRightCenter = bottomRightCenter - tree.WorldRect.min;
                int bottomRightRow = Mathf.FloorToInt(bottomRightCenter.y / childeLength);
                int bottomRightColumn = Mathf.FloorToInt(bottomRightCenter.x / childeLength);
                BottomRightIndex = bottomRightRow * childSideNodeCount + bottomRightColumn;
            }
           
            if (NodeLevel != 0)
            {
                float parentLength = nodeLength * 2f;
                int parentSideNodeCount = (int)Mathf.Sqrt(Mathf.Pow(4, NodeLevel - 1));
                Vector2 posInParent = nodeRect.center;
                posInParent = posInParent - tree.WorldRect.min;
                int parentRow = Mathf.FloorToInt(posInParent.y / parentLength);
                int parentColumn = Mathf.FloorToInt(posInParent.x / parentLength);
                ParentIndex = parentRow * parentSideNodeCount + parentColumn;
            }
            
        }

        public GridQuadTreeNode(GridQuadTree<T> tree,Rect nodeRect,int nodeLevel,bool isLeaf, Action<GridQuadTreeItem<T>, GridQuadTreeNode<T>> OnMove)
        {
            IsLeaf = isLeaf;
            this.tree = tree;
            this.nodeRect = nodeRect;
            this.NodeLevel = nodeLevel;
            looseRect = new Rect(nodeRect.min + (1 - tree.looseFactor) * (nodeRect.size / 2f), nodeRect.size * tree.looseFactor);
            ChildrenItemCount = 0;
			items = new List<GridQuadTreeItem<T>>();
            Init();
            Move += OnMove;

        }

		public void Insert(GridQuadTreeItem<T> item)
		{
			item.Destroy += ItemDestroy;
			item.Move += ItemMove;
			items.Add (item);
		}

		private void ItemMove(GridQuadTreeItem<T> item)
		{
            if (looseRect.Contains(item.ItemRect))
                return;
            if (items.Contains(item))
			{
                if (Move != null)
                    Move(item,this);
                RemoveItem(item);
            }
        }

		private void ItemDestroy(GridQuadTreeItem<T> item)
		{
			RemoveItem(item);
		}

		protected void RemoveItem(GridQuadTreeItem<T> item)
		{
			if (items.Contains(item))
			{
				item.Move -= ItemMove;
				item.Destroy -= ItemDestroy;
				items.Remove(item);
			}
		}

		public void GetItems(Rect resultRect, ref List<GridQuadTreeItem<T>> ItemsFound)
		{
			
			if (looseRect.Overlaps(resultRect))
			{
				foreach (GridQuadTreeItem<T> Item in items)
				{
					if (Item.ItemRect.Overlaps(resultRect)) 
						ItemsFound.Add(Item);
				}
					
			}
		} 
			
    }

    public class GridQuadTreeItem<T>
    {
		
		public event Action<GridQuadTreeItem<T>> Move;

		public event Action<GridQuadTreeItem<T>> Destroy;


		protected void OnMove()
		{
			if (Move != null) Move(this);
		}


		protected void OnDestroy()
		{
			if (Destroy != null) Destroy(this);
		}
			
		public Vector2 Position
		{
			get { return itemRect.center; }
			set
			{
				itemRect.center  = value;
				OnMove();
			}
		}


		public Vector2 Size
		{
			get { return itemRect.size; }
			set
			{
				itemRect.size = value;
				OnMove();
			}
		}


		private Rect itemRect;

		public Rect ItemRect
		{
			get { return itemRect; }
		}
			
		public T HoldObject
		{
			get;
			private set;
		}
			
		public GridQuadTreeItem(T obj, Vector2 position, Vector2 size)
		{
			itemRect = Rect.zero;
			itemRect.size = size;
            itemRect.center = position;
            HoldObject = obj;
			
		}

	
		public void Delete()
		{
			OnDestroy();
		}


	}
    
}

