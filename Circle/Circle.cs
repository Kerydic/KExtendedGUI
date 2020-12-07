public class Circle{
	private static Vector3 GetRightCenter(Vector3 p1, Vector3 p2, float radius)
	{
		var distance = Vector2.Distance(p1, p2);
		if (distance == 0)
		{
			return p1;
		}
		if (distance > 2 * radius)
		{
			Debug.LogError("Invalid Radius: diameter is shorter than distance!");
			return Vector2.zero;
		}
		if (Mathf.Abs(p1.y - p2.y) < 0.001)
		{
			var h = Mathf.Sqrt(radius * radius - (p1.x - p2.x) * (p1.x - p2.x) / 4);
			// 如果两个点y值相同，返回上方的点
			return new Vector2((p1.x + p2.x) / 2, p1.y + h);
		}
		if (Mathf.Abs(p1.x - p2.x) < 0.001)
		{
			var w = Mathf.Sqrt(radius * radius - (p1.y - p2.y) * (p1.y - p2.y) / 4);
			// 如果两个点y值相同，返回上方的点
			return new Vector2(p1.x + w, (p1.y + p2.y) / 2);
		}
		var k = (p1.x - p2.x) / (p2.y - p1.y);
		var midX = (p1.x + p2.x) / 2;
		var midY = (p1.y + p2.y) / 2;
		var dis = Mathf.Sqrt(radius * radius - Mathf.Pow(Vector3.Distance(p1, p2) / 2, 2));
		var angle = Mathf.Atan(k);
		// 返回靠右的那个点
		var diffX = dis * Mathf.Cos(angle);
		return diffX > 0
			? new Vector3(midX + diffX, midY + dis * Mathf.Sin(angle))
			: new Vector3(midX - diffX, midY - dis * Mathf.Sin(angle));
	}
}