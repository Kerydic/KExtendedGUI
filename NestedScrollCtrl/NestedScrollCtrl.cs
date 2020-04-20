using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
	public class NestedScrollCtrl : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler,
		IDragHandler
	{
		private enum DragType { OuterUp, OuterDown, InnerUp, InnerDown, OriginDown };

		[SerializeField] private ScrollRect _outerRect;
		[SerializeField] private ScrollRect _innerRect;

		// 判断ScrollRect是否滑动到底部时的容错值
		private float _tolerance = 0.001f;
		private float _upTolerance;
		private bool _isDragging;
		private DragType _dragType;
		private DragType dragType
		{
			get => _dragType;
			set
			{
				DragType prevType = _dragType;
				_dragType = value;
				if (_dragType == prevType)
				{
					return;
				}
				switch (value)
				{
					case DragType.OuterUp:
					case DragType.OuterDown:
						_innerRect.OnEndDrag(_currentEventData);
						_outerRect.OnBeginDrag(_currentEventData);
						break;
					case DragType.InnerUp:
					case DragType.InnerDown:
					case DragType.OriginDown:
						_outerRect.OnEndDrag(_currentEventData);
						_innerRect.OnBeginDrag(_currentEventData);
						break;
					default:
						Debug.LogError("Invalid Drag Type: " + dragType);
						break;
				}
			}
		}
		private PointerEventData _currentEventData;

		// 获取顶部元素原始状态和预期最终状态
		[SerializeField] private GameObject[] _fadeObjects;
		private float[] _fadeStarts;
		[SerializeField] private float[] _fadeTargets;
		[SerializeField] private GameObject[] _moveObjects;
		[SerializeField] private float[] _finalPos;
		private Graphic[] _graphics;
		private Vector3[] _origins;

		private void Start()
		{
			_upTolerance = 1 - _tolerance;
			_isDragging = false;

			var fadeLength = _fadeObjects.Length;
			var moveLength = _moveObjects.Length;
			_graphics = new Graphic[fadeLength];
			_fadeStarts = new float[fadeLength];
			_origins = new Vector3[moveLength];

			for (var i = 0; i < fadeLength; i++)
			{
				_graphics[i] = _fadeObjects[i].GetComponent<Graphic>();
				_fadeStarts[i] = GetAlpha(_graphics[i]);
			}

			for (var i = 0; i < moveLength; i++)
			{
				_origins[i] = _moveObjects[i].transform.localPosition;
			}
		}

		private void Update()
		{
			if (!_isDragging) return;

			UpdateTopElements();
		}

		private void SetAlpha(Graphic graphic, float alpha)
		{
			if (!graphic) return;
			Color color = graphic.color;
			color.a = alpha;
			graphic.color = color;
		}

		private float GetAlpha(Graphic graphic)
		{
			if (!graphic) return 0.0f;
			return graphic.color.a;
		}

		private void UpdateTopElements()
		{
			var outerState = _outerRect.verticalNormalizedPosition;
			for (var i = 0; i < _graphics.Length; i++)
			{
				SetAlpha(_graphics[i], _fadeStarts[i] + (1 - outerState) * (_fadeTargets[i] - _fadeStarts[i]));
			}

			for (var i = 0; i < _origins.Length; i++)
			{
				var curr = _origins[i].y + (1 - outerState) * (_finalPos[i] - _origins[i].y);
				_moveObjects[i].transform.localPosition = new Vector3(_moveObjects[i].transform.localPosition.x, curr, 0);
			}
		}

		public void ResetScrollIndex()
		{
			_outerRect.verticalNormalizedPosition = 1;
			_innerRect.verticalNormalizedPosition = 0;

			if (_graphics != null)
			{
				foreach (var graphic in _graphics)
				{
					SetAlpha(graphic, 1.0f);
				}
			}

			if (_origins != null)
			{
				for (var i = 0; i < _origins.Length; i++)
				{
					_moveObjects[i].transform.localPosition = _origins[i];
				}
			}
		}

		private void SetDragType(float deltaY)
		{
			if (deltaY > 0)
			{
				dragType = _outerRect.verticalNormalizedPosition < _tolerance ? DragType.InnerUp : DragType.OuterUp;
			}
			else
			{
				if (_innerRect.verticalNormalizedPosition < _upTolerance)
				{
					dragType = DragType.InnerDown;
				}
				else if (_outerRect.verticalNormalizedPosition < _upTolerance)
				{
					dragType = DragType.OuterDown;
				}
				else
				{
					dragType = DragType.OriginDown;
				}
			}
		}

		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			_currentEventData = eventData;

			SetDragType(eventData.delta.y);

			switch (dragType)
			{
				case DragType.InnerUp:
				case DragType.InnerDown:
				case DragType.OriginDown:
					_innerRect.OnInitializePotentialDrag(eventData);
					break;
				case DragType.OuterUp:
				case DragType.OuterDown:
					_outerRect.OnInitializePotentialDrag(eventData);
					break;
				default:
					Debug.LogError("Invalid Drag Type: " + dragType);
					break;
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			_currentEventData = eventData;

			SetDragType(eventData.delta.y);

			switch (dragType)
			{
				case DragType.InnerUp:
				case DragType.InnerDown:
				case DragType.OriginDown:
					_innerRect.OnBeginDrag(eventData);
					break;
				case DragType.OuterUp:
				case DragType.OuterDown:
					_outerRect.OnBeginDrag(eventData);
					break;
				default:
					Debug.LogError("Invalid Drag Type: " + dragType);
					break;
			}

			_isDragging = true;
		}

		public void OnDrag(PointerEventData eventData)
		{
			_currentEventData = eventData;

			SetDragType(eventData.delta.y);

			switch (dragType)
			{
				case DragType.InnerUp:
				case DragType.InnerDown:
				case DragType.OriginDown:
					_innerRect.OnDrag(eventData);
					break;
				case DragType.OuterUp:
				case DragType.OuterDown:
					_outerRect.OnDrag(eventData);
					break;
				default:
					Debug.LogError("Invalid Drag Type: " + dragType);
					break;
			}
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			_currentEventData = eventData;

			SetDragType(eventData.delta.y);

			switch (dragType)
			{
				case DragType.InnerUp:
				case DragType.InnerDown:
				case DragType.OriginDown:
					_innerRect.OnEndDrag(eventData);
					break;
				case DragType.OuterUp:
				case DragType.OuterDown:
					_outerRect.OnEndDrag(eventData);
					break;
				default:
					Debug.LogError("Invalid Drag Type: " + dragType);
					break;
			}

			_isDragging = false;
		}
	}
}
