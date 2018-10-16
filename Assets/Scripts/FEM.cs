using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using LUD;

public class FEM : MonoBehaviour
{
	//public Material mat;
	private int div = 6;
	public Vector2[] pos;
	public Vector2[] stress;

	void Start()
	{
		var loop = new[] { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f };
		var vertices = (from y in loop
						from x in loop
						select new Vector3(x, y, 0.0f)).ToArray();

		/*foreach(Vector3 vertex in vertices) {
			Debug.Log(vertex.x);
			Debug.Log(vertex.y);
			Debug.Log(vertex.z);
		}*/
		
		var indices1 = (from i in Enumerable.Range(0, div - 1)
					   from j in Enumerable.Range(0, div - 1)
					   select new int[] { div * i + j, div * i + j + 1, div * i + div + j }).ToArray();
		
		var indices2 = (from i in Enumerable.Range(0, div - 1)
					   from j in Enumerable.Range(0, div - 1)
					   select new int[] { div * i + j + 1, div * i + div + j + 1, div * i + div + j }).ToArray();
		 
		var indices = indices1.Concat(indices2).ToArray();
		/*
		foreach (int[] index in indices) {
			Debug.Log(index[0]);
			Debug.Log(index[1]);
			Debug.Log(index[2]);
		}*/
		
		var E = 100;// ヤング率
		var Nu = 0.5;// ポアソン比
		var _Nu = 1 - Nu * Nu;
		var G = E/(2 * (1.0 - Nu));// せん断弾性係数
		var delta = 0.5;
		double a1, a2, a3, b1, b2, b3, c1, c2, c3;
		double x1, x2, x3, y1, y2, y3;
	
		double[,] BT = new double[6, 3];
		double[,] D = { { E / _Nu, Nu * E / _Nu, 0.0 }, { Nu * E / _Nu, E / _Nu, 0.0 }, { 0.0, 0.0, G } };
		double[,] B = new double[3, 6];
		double[,] tmp = new double[6, 3];
		double[,] k = new double[6, 6];
		double[,] K = new double[2 * div * div, 2 * div * div];
		double[] F = new double[2 * div * div];

		foreach (int[] index in indices)
		{
			x1 = vertices[index[0]].x;
			y1 = vertices[index[0]].y;
			x2 = vertices[index[1]].x;
			y2 = vertices[index[1]].y;
			x3 = vertices[index[2]].x;
			y3 = vertices[index[2]].y;

			a1 = x2 * y3 - x3 * y2;
			a2 = x3 * y1 - x1 * y3;
			a3 = x1 * y2 - x2 * y1;
			b1 = y2 - y3;
			b2 = y3 - y1;
			b3 = y1 - y2;
			c1 = x3 - x2;
			c2 = x1 - x3;
			c3 = x2 - x1;

			B = new double[,] { { b1, 0.0, b2, 0.0, b3, 0.0 }, { 0.0, c1, 0.0, c2, 0.0, c3 }, { c1, b1, c2, b2, c3, b3 } };
			BT = new double[,] { { b1, 0.0, c1 }, { 0.0, c1, b1 }, { b2, 0.0, c2 }, { 0.0, c2, b2 }, { b3, 0.0, c3 }, { 0.0, c3, b3 } };
			
			// 要素剛性マトリクスの計算
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					for (int _k = 0; _k < 3; _k++)
					{
						tmp[i, j] += BT[i, _k] * D[_k, j];
					}
				}
			}

			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					for (int _k = 0; _k < 3; _k++)
					{
						k[i, j] += tmp[i, _k] * B[_k, j];
					}
				}
			}

			// 全体剛性マトリクスの導出		
			for (int i = 0; i < 6; i++)
			{
				for (int j = 0; j < 6; j++)
				{
					//Debug.Log(2 * index[i / 2] + i % 2);
					//Debug.Log(2 * index[j / 2] + j % 2);
					K[2 * index[i / 2] + i % 2, 2 * index[j / 2] + j % 2] = k[i, j];
				}
			}
			//Debug.Log("Loop_End");
		}

		int[] pos_index = new int[2];
		// 全体荷重ベクトルの計算
		for (int i = 0; i < pos.Length; i++)
		{
			pos_index[0] = (int)pos[i].x * (div - 1);
			pos_index[1] = (int)pos[i].y * (div - 1);

			F[2 * (pos_index[0] * div + pos_index[1])] = stress[i].x;
			F[2 * (pos_index[0] * div + pos_index[1]) + 1] = stress[i].y;
		}


		
		/*foreach (double element in K)
		{
			Debug.Log(element);
		}*/
		LUDecomposition lud = new LUDecomposition();

		double[] solutionX = new double[2 * div * div];
		solutionX = lud.Solve(K, F, 2 * div * div);
		Debug.Log("Start");
		lud.WriteVector(solutionX);
	}
}
