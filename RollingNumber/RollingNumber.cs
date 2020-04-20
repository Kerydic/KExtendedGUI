using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Framework;
using DOTween = DG.Tweening.DOTween;

/**
public static void SetAnchoredPosY(this RectTransform rectTransform, float y)
{
	if(rectTransform)
		rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
}
**/

namespace Game
{
	public class RollingNumber : MonoBehaviour
	{
		private List<Text> _numberList1;    // 数字Text列表1，开始时用来保存显示的数字
		private List<Text> _numberList2;    // 数字Text列表2，开始时用来保存待替换的数字

		[SerializeField]
		private float _duration;            // 动画时长
		[SerializeField]
		private float _interval;            // 每个数字开始滚动的间隔
		[SerializeField]
		private float _offset;				// 设置间隔后如果因为位图大小原因造成整个数字不居中，使用这个值来调整
		[SerializeField]
		private float _numberSpace;         // 每个数字之间的间隔
		[SerializeField]
		private float _punctuationSpace;    // 每个标点的占位
		[SerializeField]
		private int _increment;             // 数字滚动时每一位初始值比最终值大多少，设置这个是为了保证每一位数字滚动的时长都相同，保证动画的协调性
		[SerializeField]
		private float _bufferTime;          // 数字最后一次滚动的缓冲时间
		[SerializeField]
		private float _harmonicTime;		// 数字最后一次滚动的回弹时间
		[SerializeField]
		private float _bufferLength;		// 数字最后一次滚动的回弹长度
		private float _speed;               // 每个数字滚动的速度
		private bool _isRolling;            // 当前是否正在滚动
		private float _startTime;
		private int _maxCount;
		private bool _fromZero = false;		// SetNumber的fromNumber参数是否为0
		private float _lengthDelta = -1.0f; // SetNumber有fromNumber参数时，数字长度的变化
		[SerializeField]
		private float _tweakTime;			// 变化完成后数字左右移动的时长
		private int _currentNumberIndex;

		private int[] _restRollingTimes;

		private Vector2 _originSize;
		private Text _originText;
		private Text _toShowText;

		public Action onComplete;           // 滚动完成的回调

		void Awake()
		{
			_originSize = transform.GetComponent<RectTransform>().sizeDelta;

			_originText = transform.Find("Origin").GetComponent<Text>();
			_toShowText = transform.Find("ToShow").GetComponent<Text>();

			_originText.rectTransform.sizeDelta = new Vector2(_numberSpace, _originSize.y);
			_toShowText.rectTransform.sizeDelta = new Vector2(_numberSpace, _originSize.y);

			_numberList1 = new List<Text>();
			_numberList2 = new List<Text>();

			if (_increment > 9)
				Debug.LogError("Increment count not be bigger that 9!");

			_speed = _increment * _originSize.y / _duration;
		}

		void FixedUpdate()
		{
			if (!_isRolling) return;

			if (_currentNumberIndex < _maxCount && Time.time >= _startTime + _currentNumberIndex * _interval)
			{
				_currentNumberIndex += 1;
			}

			for (int i = 0; i < _currentNumberIndex; i++)
			{
				if (_restRollingTimes[i] < 0) continue;

				Text number1 = _numberList1[i];
				Text number2 = _numberList2[i];
				number1.rectTransform.SetAnchoredPosY(_originSize.y - (_originSize.y - (number1.rectTransform.anchoredPosition.y - Time.deltaTime * _speed)) % (2 * _originSize.y));
				if (number1.rectTransform.anchoredPosition.y - Time.deltaTime * _speed <= -_originSize.y && _restRollingTimes[i] >= 0)
				{
					_restRollingTimes[i] -= 1;
					if (_restRollingTimes[i] < 0)
					{
						number1.transform.DOLocalMoveY(-_originSize.y, _bufferTime);
						var s = DOTween.Sequence();
						s.Append(number2.transform.DOLocalMoveY(-_bufferLength, _bufferTime)).Append(number2.transform.DOLocalMoveY(0, _harmonicTime).SetEase(Ease.OutCubic));
						continue;
					}
					number1.rectTransform.SetAnchoredPosY(number1.rectTransform.anchoredPosition.y + 2 * _originSize.y);
					number1.text = NextNumber(number2.text);
				}
				number2.rectTransform.SetAnchoredPosY(_originSize.y - (_originSize.y - (number2.rectTransform.anchoredPosition.y - Time.deltaTime * _speed)) % (2 * _originSize.y));
				if (number2.rectTransform.anchoredPosition.y - Time.deltaTime * _speed <= -_originSize.y && _restRollingTimes[i] >= 0)
				{
					_restRollingTimes[i] -= 1;
					if (_restRollingTimes[i] < 0)
					{
						number2.transform.DOLocalMoveY(-_originSize.y, _bufferTime);
						var s = DOTween.Sequence();
						s.Append(number1.transform.DOLocalMoveY(-_bufferLength, _bufferTime)).Append(number1.transform.DOLocalMoveY(0, _harmonicTime).SetEase(Ease.OutCubic));
						continue;
					}
					number2.rectTransform.SetAnchoredPosY(number2.rectTransform.anchoredPosition.y + 2 * _originSize.y);
					number2.text = NextNumber(number1.text);
				}
			}

			if ((_maxCount > 1 && _restRollingTimes[_maxCount - 2] < 0 && _restRollingTimes[_maxCount - 1] < 0) ||(_maxCount == 1 && _restRollingTimes[0] < 0))
			{
				if (!_fromZero)
				{
					transform.DOLocalMoveX(_lengthDelta / 2, _tweakTime).SetEase(Ease.OutCubic);
				}

				onComplete?.Invoke();
				_isRolling = false;
			}
		}

		private string NextNumber(string prev)
		{
			if (int.TryParse(prev, out int a))
			{
				return ((a + 9) % 10).ToString();
			}
			return "";
		}

		private void ClearNumberLists()
		{
			foreach (Text i in _numberList1)
			{
				Destroy(i.gameObject);
			}
			foreach (Text i in _numberList2)
			{
				Destroy(i.gameObject);
			}
			_numberList1.Clear();
			_numberList2.Clear();
		}

		private float GetTotalLength(int count)
		{
			int punctuationNum;
			if(count % 3 == 0)
			{
				punctuationNum = count / 3 - 1;
			}
			else
			{
				punctuationNum = (count - count % 3) / 3;
			}
			return (count + 1) * _numberSpace + punctuationNum * _punctuationSpace;
		}

		public void SetNumbers(int toNumber, int fromNumber = 0)
		{
			ClearNumberLists();

			_fromZero = fromNumber == 0;

			char[] toNumberArray = toNumber.ToString().ToCharArray();
			Array.Reverse(toNumberArray);
			int count1 = toNumberArray.Length;
			char[] fromNumberArray = fromNumber.ToString().ToCharArray();
			Array.Reverse(fromNumberArray);
			int count2 = fromNumberArray.Length;

			Text originTextItem, toShowTextItem;
			_maxCount = Math.Max(count1, count2);

			float previousPosX;
			if (fromNumber == 0)
				previousPosX = GetTotalLength(_maxCount) / 2 + _numberSpace / 2 + _offset;
			else
				previousPosX = GetTotalLength(count2) / 2 + _numberSpace / 2 + _offset;
			_lengthDelta = GetTotalLength(count1) - GetTotalLength(count2);

			int punctuation = 0;
			for (int i = 0; i < _maxCount; i++)
			{
				originTextItem = Instantiate(_originText, transform);
				toShowTextItem = Instantiate(_toShowText, transform);
				RectTransform originRect = originTextItem.rectTransform;
				RectTransform toShowRect = toShowTextItem.rectTransform;

				if (i % 4 == 3 && i != 0)
				{
					punctuation += 1;
					_maxCount += 1;

					previousPosX -= (_numberSpace + _punctuationSpace) / 2;

					originRect.sizeDelta = new Vector2(_punctuationSpace, _originSize.y);
					toShowRect.sizeDelta = new Vector2(_punctuationSpace, _originSize.y);
					originRect.localPosition = new Vector2(previousPosX, 0);
					toShowRect.localPosition = new Vector2(previousPosX, _originSize.y);

					originTextItem.gameObject.SetActive(true);
					toShowTextItem.gameObject.SetActive(true);

					originTextItem.text = fromNumber == 0 ? "" : ",";
					toShowTextItem.text = ",";

					previousPosX += (_numberSpace - _punctuationSpace) / 2;
				}
				else
				{
					previousPosX -= _numberSpace;

					originRect.localPosition = new Vector2(previousPosX, 0);
					toShowRect.localPosition = new Vector2(previousPosX, _originSize.y);
					originTextItem.gameObject.SetActive(true);
					toShowTextItem.gameObject.SetActive(true);

					if (count1 < count2)
					{
						// 暂时只考虑从小变大的情况，从大变小不改
						originTextItem.text = fromNumberArray[i - punctuation].ToString();
						toShowTextItem.text = i - punctuation < count1 ? ((int.Parse(toNumberArray[i - punctuation].ToString()) + _increment) % 10).ToString() : (0 + _increment).ToString();
					}
					else
					{
						if (i - punctuation < count2)
						{
							originTextItem.text = fromNumberArray[i - punctuation].ToString();
						}
						else if (i - punctuation == count2)
						{
							originTextItem.text = _fromZero ? "" : "$";
						}
						else
						{
							originTextItem.text = "";
						}
						toShowTextItem.text = ((int.Parse(toNumberArray[i - punctuation].ToString()) + _increment) % 10).ToString();
					}
				}

				_numberList1.Add(originTextItem);
				_numberList2.Add(toShowTextItem);
			}

			originTextItem = Instantiate(_originText, transform);
			toShowTextItem = Instantiate(_toShowText, transform);

			originTextItem.rectTransform.localPosition = new Vector2(previousPosX - _numberSpace, 0);
			toShowTextItem.rectTransform.localPosition = new Vector2(previousPosX - _numberSpace, _originSize.y);
			originTextItem.gameObject.SetActive(true);
			toShowTextItem.gameObject.SetActive(true);

			originTextItem.text = count1 > count2 ? "" : "$";
			toShowTextItem.text = count1 >= count2 ? "$" : "";
			_numberList1.Add(originTextItem);
			_numberList2.Add(toShowTextItem);
			_maxCount += 1;

			_isRolling = true;
			_restRollingTimes = new int[_maxCount];
			for (int i = 0; i < _maxCount - 1; i++)
			{
				if (i != 0 && i % 4 == 3)
				{
					_restRollingTimes[i] = 0;
					continue;
				}
				_restRollingTimes[i] = _increment;
			}
			_restRollingTimes[_maxCount - 1] = 0;

			_startTime = Time.time;
			_currentNumberIndex = 0;
		}

		public void Test()
		{
			SetNumbers(150000, 75000);
		}
	}
}