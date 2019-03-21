using UnityEngine;
using System.Collections.Generic;

public class Liquid : MonoBehaviour {

    [Header("最大最小液体体积")]
    public float MaxValue = 1.0f;
    public float MinValue = 0.005f;

    [Header("液体上方可以残留的最大液体量")]
    public float MaxCompression = 0.25f;

    [Header("每次迭代允许流动的最低和最高液体量")]
    public float MinFlow = 0.005f;
    public float MaxFlow = 4f;

    [Header("基础速度")]
    [Range(0f,1f)]
	public float FlowSpeed = 1f;

	// 跟踪液体数组
	float[,] Diffs;
    /// <summary>
    /// 初始化液体数组
    /// </summary>
    /// <param name="cells"></param>
	public void Initialize(Cell[,] cells) {
		Diffs = new float[cells.GetLength (0), cells.GetLength (1)];
	}

    /// <summary>
    /// 下面是计算如何在两个垂直相邻的单元之间分配给定水量的函数
    /// </summary>
    /// <param name="remainingLiquid"></param>
    /// <param name="destination"></param>
    /// <returns></returns>
    float CalculateVerticalFlowValue(float remainingLiquid, Cell destination)
	{
		float sum = remainingLiquid + destination.Liquid;
		float value = 0;
        //如果总量小于最大值，直接流动最大值,话说为什么是返回最大值
		if (sum <= MaxValue) {
			value = MaxValue;

		} else if (sum < 2 * MaxValue + MaxCompression) {
			value = (MaxValue * MaxValue + sum * MaxCompression) / (MaxValue + MaxCompression);
		} else {
			value = (sum + MaxCompression) / 2f;
		}

		return value;
	}

    //模拟流动
	public void Simulate(ref Cell[,] cells) {

		float flow = 0;

		// 清空流体数组
		for (int x = 0; x < cells.GetLength (0); x++) {
			for (int y = 0; y < cells.GetLength (1); y++) {
				Diffs [x, y] = 0;
			}
		}

		// 主循环
		for (int x = 0; x < cells.GetLength(0); x++) {
			for (int y = 0; y < cells.GetLength(1); y++) {

				//从数组拿到细胞，清空方向
				Cell cell = cells [x, y];
				cell.ResetFlowDirections ();

				//判断格子类型
				if (cell.Type == CellType.Solid) {
					cell.Liquid = 0;
					continue;
				}
                //没有液体
				if (cell.Liquid == 0)
					continue;
                //如果已经稳定
				if (cell.Settled) 
					continue;
                //如果小于最小值
				if (cell.Liquid < MinValue) {
					cell.Liquid = 0;
					continue;
				}

				//跟踪当前格子的液体
				float startValue = cell.Liquid;
				float remainingValue = cell.Liquid;
				flow = 0;

                Button(cell,x,y,ref flow,ref remainingValue);
				// 小于最小值直接抹掉
				if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}

                Left(cell, x, y, ref flow, ref remainingValue);
                // 小于最小值直接抹掉
                if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}

                Right(cell, x, y, ref flow, ref remainingValue);
                // 小于最小值直接抹掉
                if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}

                Top(cell, x, y, ref flow, ref remainingValue);
                // 小于最小值直接抹掉
                if (remainingValue < MinValue) {
					Diffs [x, y] -= remainingValue;
					continue;
				}

				// 如果不在流动，就设为稳定状态
				if (startValue == remainingValue) {
					cell.SettleCount++;
					if (cell.SettleCount >= 10) {
						cell.ResetFlowDirections ();
						cell.Settled = true;
					}
				} else {
                    //向周围格子流动
					cell.UnsettleNeighbors ();
				}
			}
		}
			
		//更新格子数组的液体
		for (int x = 0; x < cells.GetLength (0); x++) {
			for (int y = 0; y < cells.GetLength (1); y++) {
				cells [x, y].Liquid += Diffs [x, y];
				if (cells [x, y].Liquid < MinValue) {
					cells [x, y].Liquid = 0;
					cells [x, y].Settled = false;	//空格子都是可以流动的
				}				
			}
		}			
	}
    private void Button(Cell cell, int x,int y,ref float flow,ref float remainingValue)
    {
        if (cell.Bottom != null && cell.Bottom.Type == CellType.Blank)
        {

            // 计算下落速度
            flow = CalculateVerticalFlowValue(cell.Liquid, cell.Bottom) - cell.Bottom.Liquid;
            if (cell.Bottom.Liquid > 0 && flow > MinFlow)
                flow *= FlowSpeed;

            //约束速度
            flow = Mathf.Max(flow, 0);
            if (flow > Mathf.Min(MaxFlow, cell.Liquid))
                flow = Mathf.Min(MaxFlow, cell.Liquid);

            // 更新数组
            if (flow != 0)
            {
                remainingValue -= flow;
                Diffs[x, y] -= flow;
                Diffs[x, y + 1] += flow;
                cell.FlowDirections[(int)FlowDirection.Bottom] = true;
                cell.Bottom.Settled = false;
            }
        }
    }
    private void Left(Cell cell, int x, int y, ref float flow, ref float remainingValue)
    {
        if (cell.Left != null && cell.Left.Type == CellType.Blank)
        {

            // 计算速度
            flow = (remainingValue - cell.Left.Liquid) / 4f;
            if (flow > MinFlow)
                flow *= FlowSpeed;

            // 约束
            flow = Mathf.Max(flow, 0);
            if (flow > Mathf.Min(MaxFlow, remainingValue))
                flow = Mathf.Min(MaxFlow, remainingValue);

            // 更新
            if (flow != 0)
            {
                remainingValue -= flow;
                Diffs[x, y] -= flow;
                Diffs[x - 1, y] += flow;
                cell.FlowDirections[(int)FlowDirection.Left] = true;
                cell.Left.Settled = false;
            }
        }
    }
    private void Right(Cell cell, int x, int y, ref float flow, ref float remainingValue)
    {
        if (cell.Right != null && cell.Right.Type == CellType.Blank)
        {

            //计算速度
            flow = (remainingValue - cell.Right.Liquid) / 4f;
            if (flow > MinFlow)
                flow *= FlowSpeed;

            // 约束
            flow = Mathf.Max(flow, 0);
            if (flow > Mathf.Min(MaxFlow, remainingValue))
                flow = Mathf.Min(MaxFlow, remainingValue);

            // 更新
            if (flow != 0)
            {
                remainingValue -= flow;
                Diffs[x, y] -= flow;
                Diffs[x + 1, y] += flow;
                cell.FlowDirections[(int)FlowDirection.Right] = true;
                cell.Right.Settled = false;
            }
        }
    }
    private void Top(Cell cell, int x, int y, ref float flow, ref float remainingValue)
    {
        if (cell.Top != null && cell.Top.Type == CellType.Blank)
        {
            //计算速度
            flow = remainingValue - CalculateVerticalFlowValue(remainingValue, cell.Top);
            if (flow > MinFlow)
                flow *= FlowSpeed;

            // 约束
            flow = Mathf.Max(flow, 0);
            if (flow > Mathf.Min(MaxFlow, remainingValue))
                flow = Mathf.Min(MaxFlow, remainingValue);

            // 更新液体数组
            if (flow != 0)
            {
                remainingValue -= flow;
                Diffs[x, y] -= flow;
                Diffs[x, y - 1] += flow;
                cell.FlowDirections[(int)FlowDirection.Top] = true;
                cell.Top.Settled = false;
            }
        }
    }
}
