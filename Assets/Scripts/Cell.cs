using UnityEngine;
/// <summary>
/// 空白，实体
/// </summary>
public enum CellType {
	Blank,
	Solid
}
/// <summary>
/// 流向 上右下左↑→↓←
/// </summary>
public enum FlowDirection { 
	Top = 0, 
	Right = 1,
	Bottom = 2, 
	Left = 3
}

public class Cell : MonoBehaviour {
    
	// Grid index reference
	public int X { get ; private set; }
	public int Y { get; private set; }

	// Amount of liquid in this cell
	public float Liquid { get; set; }

	// Determines if Cell liquid is settled
	private bool _settled;
	public bool Settled { 
		get { return _settled; } 
		set {
			_settled = value; 
			if (!_settled) {
				SettleCount = 0;
			}
		}
	}
	public int SettleCount { get; set; }

	public CellType Type { get; private set; }

	// 邻居
	public Cell Top;
	public Cell Bottom { get; set; }
	public Cell Left { get; set; }
	public Cell Right { get; set; }

	// Shows flow direction of cell
	public int Bitmask { get; set; }
	public bool[] FlowDirections = new bool[4];

	// 水体颜色
	public Color Color;
	public Color DarkColor = new Color (0, 0.1f, 0.2f, 1);

	SpriteRenderer BackgroundSprite;
	SpriteRenderer LiquidSprite;
	SpriteRenderer FlowSprite;

	Sprite[] FlowSprites;

	bool ShowFlow;
	bool RenderDownFlowingLiquid;
	bool RenderFloatingLiquid;

	void Awake() {
		BackgroundSprite = transform.Find ("Background").GetComponent<SpriteRenderer> ();
		LiquidSprite = transform.Find ("Liquid").GetComponent<SpriteRenderer> ();
		FlowSprite = transform.Find ("Flow").GetComponent<SpriteRenderer> ();
		//Color = LiquidSprite.color;
	}

	public void Set(int x, int y, Vector2 position, float size, Sprite[] flowSprites, bool showflow, bool renderDownFlowingLiquid, bool renderFloatingLiquid) {
		
		X = x;
		Y = y;

		RenderDownFlowingLiquid = renderDownFlowingLiquid;
		RenderFloatingLiquid = renderFloatingLiquid;
		ShowFlow = showflow;
		FlowSprites = flowSprites;
		transform.position = position;
		transform.localScale = new Vector2 (size, size);

		FlowSprite.sprite = FlowSprites [0];
	}
	/// <summary>
    /// 设置格子类型
    /// </summary>
    /// <param name="type"></param>
	public void SetType(CellType type) {
		Type = type;
		if (Type == CellType.Solid) {
			Liquid = 0;
		}
		UnsettleNeighbors ();
	}
    /// <summary>
    /// 添加水
    /// </summary>
    /// <param name="amount"></param>
	public void AddLiquid(float amount) {
		Liquid += amount;
		Settled = false;
	}

	public void ResetFlowDirections() {
		FlowDirections [0] = false;
		FlowDirections [1] = false;
		FlowDirections [2] = false;
		FlowDirections [3] = false;
	}

	// Force neighbors to simulate on next iteration
	public void UnsettleNeighbors() {
		if (Top != null)
			Top.Settled = false;
		if (Bottom != null)
			Bottom.Settled = false;
		if (Left != null)
			Left.Settled = false;
		if (Right != null)
			Right.Settled = false;
	}

	public void Update() {

		// Set background color based on cell type
		if (Type == CellType.Solid) {
			BackgroundSprite.color = Color.gray;
		} else {
			BackgroundSprite.color = Color.black;
		}

		// Update bitmask based on flow directions
		Bitmask = 0;
		if (FlowDirections [(int)FlowDirection.Top])
			Bitmask += 1;
		if (FlowDirections [(int)FlowDirection.Right])
			Bitmask += 2;
		if (FlowDirections [(int)FlowDirection.Bottom])
			Bitmask += 4;
		if (FlowDirections [(int)FlowDirection.Left])
			Bitmask += 8;
		
		if (ShowFlow) {
			// 显示方向
			FlowSprite.sprite = FlowSprites [Bitmask];
		} else {
			FlowSprite.sprite = FlowSprites [0];
		}

		//根据水的数量调整图片大小
		LiquidSprite.transform.localScale = new Vector2 (1, Mathf.Min (1, Liquid));	

		// Optional rendering flags
		if (!RenderFloatingLiquid) {
			// 如果下面有残留的水，清空图片
			if (Bottom != null && Bottom.Type != CellType.Solid && Bottom.Liquid <= 0.99f) {
				LiquidSprite.transform.localScale = new Vector2 (0, 0);	
			}
		}
		if (RenderDownFlowingLiquid) {
			// 如果上面有水，填满一个格子
			if (Type == CellType.Blank && Top != null && (Top.Liquid > 0.05f || Top.Bitmask == 4)) {
				LiquidSprite.transform.localScale = new Vector2 (1, 1);	
			}
		}

		// 根据水的数量绘制颜色
		LiquidSprite.color = Color.Lerp (Color, DarkColor, Liquid / 4f);
	}

}
