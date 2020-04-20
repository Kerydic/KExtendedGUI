using Framework;
using LuaInterface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
	public class FullWidthSwitcher : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
	{
		[SerializeField] private GameObject reference;
		[SerializeField] private GameObject obj1;
		[SerializeField] private GameObject obj2;
		[SerializeField] private GameObject obj3;
		[SerializeField] private float elasticity = 0.1f;
		[SerializeField] private bool inertia = true;
		[SerializeField] private float decelerationRate = 0.135f;
		[SerializeField] private float scrollSensitivity = 1.0f;

		[SerializeField] private bool enableFastSwipe;
		[SerializeField] private float fastSwipeThreshold = 200;

		[SerializeField] private Text progressNum;
		[SerializeField] private Button prevBtn;
		[SerializeField] private Button nextBtn;

		private int _totalCount;
		private int _prevCount;
		public int currCount;

		private bool _initialized = false;
		
		private RectTransform _viewRect;
		private RectTransform _leftObj;
		private RectTransform _midObj;
		private RectTransform _rightObj;
		private float _preferredWidth;
		private float _logicalLeftBound;
		private float _logicalRightBound;
		private float _leftViewBound;
		private float _rightViewBound;
		private Vector2 _prevPosition = Vector2.zero;

		private Vector2 _velocity;
		private bool _dragging;
		private bool _scrolling;
		private bool _autoJumping;

		private float _autoTargetNormalizedPosition;

		private Vector2 _contentStartLocation;
		private Vector2 _pointerStartLocation;

		private float logicalNormalizedPosition
		{
			get
			{
				UpdateLogicalBounds();
				if (_totalCount <= 1)
					return _logicalLeftBound < -_preferredWidth / 2 ? 0 : 1;
				return Mathf.Abs(_logicalLeftBound + _preferredWidth / 2) / (_preferredWidth * (_totalCount - 1));
			}
			set => SetLogicalNormalizedPosition(value);
		}

		LuaFunction _requestItemContentUpdate;
		LuaFunction _requestAllContentUpdate;

		protected override void Awake()
		{
			_leftObj = obj1.GetComponent<RectTransform>();
			_midObj = obj2.GetComponent<RectTransform>();
			_rightObj = obj3.GetComponent<RectTransform>();
		}

		protected override void Start()
		{
			_viewRect = GetComponent<RectTransform>();
			_preferredWidth = reference.GetComponent<RectTransform>().rect.width;
			_leftViewBound = -_preferredWidth / 2;
			_rightViewBound = _preferredWidth / 2;
			ResizeItemObjs();
			
			prevBtn.onClick.AddListener(PrevItem);
			nextBtn.onClick.AddListener(NextItem);
		}
		
		protected override void OnDestroy()
		{
			LuaFunctionHelper.Dispose(_requestItemContentUpdate);
			LuaFunctionHelper.Dispose(_requestAllContentUpdate);
		}

		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			if (!_initialized)
				return;
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			_velocity = Vector2.zero;
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (!_initialized)
				return;
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			if (!IsActive())
				return;

			UpdateLogicalBounds();
			
			_pointerStartLocation = Vector2.zero;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRect, eventData.position, eventData.pressEventCamera, out _pointerStartLocation);
			UpdateObjPosStatus();
			_contentStartLocation = _midObj.anchoredPosition;
			_dragging = true;
			_autoJumping = false;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (!_initialized)
				return;
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			_dragging = false;
			if (!enableFastSwipe) return;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRect, eventData.position, eventData.pressEventCamera, out var localCursor))
				return;
			
			UpdateLogicalBounds();
			
			var pointerDelta = localCursor - _pointerStartLocation;
			Vector2 position = _contentStartLocation + pointerDelta;
			
			Vector2 offset = CalculateOffset(position - _midObj.anchoredPosition);
			if (offset != Vector2.zero)
				return;
			
			if (_midObj.anchoredPosition.x <= - fastSwipeThreshold)
			{
				NextItem();
			}
			else if (_midObj.anchoredPosition.x >= fastSwipeThreshold)
			{
				PrevItem();
			}
			else
			{
				JumpToIndex(currCount);
			}
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (!_initialized)
				return;
			if (eventData.button != PointerEventData.InputButton.Left)
				return;
			if (!IsActive())
				return;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewRect, eventData.position, eventData.pressEventCamera, out var localCursor))
				return;
			
			UpdateLogicalBounds();
			
			var pointerDelta = localCursor - _pointerStartLocation;
			Vector2 position = _contentStartLocation + pointerDelta;
			
			Vector2 offset = CalculateOffset(position - _midObj.anchoredPosition);
			position.x += offset.x;
			if (offset != Vector2.zero)
				position.x -= RubberDelta(offset.x, _viewRect.rect.width);

			UpdateItemAnchoredPosition(position);
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (!_initialized)
				return;
			if (!IsActive())
				return;

			UpdateLogicalBounds();
			
			Vector2 delta = eventData.scrollDelta;
			// Down is positive for scroll events, while in UI system up is positive
			delta.y *= -1;
			if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
				delta.x = delta.y;
			delta.y = 0;

			if (eventData.IsScrolling())
				_scrolling = true;

			Vector2 position = _midObj.anchoredPosition;
			position += delta * scrollSensitivity;
			
			UpdateItemAnchoredPosition(position);
			UpdateLogicalBounds();
		}

		protected void LateUpdate()
		{
			if (!_initialized)
				return;
			UpdateLogicalBounds();
			float deltaTime = Time.unscaledDeltaTime;
			Vector2 offset = CalculateOffset(Vector2.zero);
			if (!_dragging && _autoJumping)// && offset == Vector2.zero)
			{
				float prevNormalizedPosition = logicalNormalizedPosition;
				float calculatedNormalizedPosition = Mathf.Lerp(prevNormalizedPosition, _autoTargetNormalizedPosition, 7.5f * Time.deltaTime);
				if (Mathf.Abs(calculatedNormalizedPosition - _autoTargetNormalizedPosition) < 0.00001f)
					_autoJumping = false;
				logicalNormalizedPosition = calculatedNormalizedPosition;
			}
			
			if (!_dragging && (offset != Vector2.zero || _velocity != Vector2.zero))
			{
				Vector2 position = _midObj.anchoredPosition;
				if (offset != Vector2.zero)
				{
					float speed = _velocity.x;
					float smoothTime = elasticity;
					if (_scrolling)
						smoothTime *= 3.0f;
					position.x = Mathf.SmoothDamp(position.x, position.x + offset.x, ref speed, smoothTime, Mathf.Infinity, deltaTime);
					if (Mathf.Abs(speed) < 1)
						speed = 0;
					_velocity.x = speed;
				}
				else if (inertia)
				{
					_velocity *= Mathf.Pow(decelerationRate, deltaTime);
					if (Mathf.Abs(_velocity.x) < 1)
						_velocity = Vector2.zero;
					position.x += _velocity.x * deltaTime;
				}
				else
				{
					_velocity = Vector2.zero;
				}
				UpdateItemAnchoredPosition(position);
			}

			if (_dragging && inertia)
			{
				Vector3 newVelocity = (_midObj.anchoredPosition - _prevPosition) / deltaTime;
				_velocity.x = Mathf.Lerp(_velocity.x, newVelocity.x, deltaTime * 10);
			}

			if (_midObj.anchoredPosition != _prevPosition)
			{
				UpdatePrevData();
			}

			_scrolling = false;
			UpdateObjPosStatus();
			progressNum.text = currCount + "/" + _totalCount;
		}

		private void UpdatePrevData()
		{
			_prevPosition = _midObj.anchoredPosition;
		}
		
		private void ResizeItemObj(GameObject obj)
		{
			RectTransform rect = obj.GetComponent<RectTransform>();
			if (!rect)
				return;
			rect.sizeDelta = new Vector2(_preferredWidth, rect.sizeDelta.y);
		}
		
		// Resize three objects according to reference object's width
		private void ResizeItemObjs()
		{
			ResizeItemObj(obj1);
			ResizeItemObj(obj2);
			ResizeItemObj(obj3);
		}

		private void SetOutBoundsItemActive()
		{
			if (currCount == 1)
			{
				GameObject leftObj = _leftObj.gameObject;
				if (leftObj.activeSelf)
					leftObj.SetActive(false);
			}
			else if (!_leftObj.gameObject.activeSelf)
				_leftObj.gameObject.SetActive(true);

			if (currCount == _totalCount)
			{
				GameObject rightObj = _rightObj.gameObject;
				if(rightObj.activeSelf)
					rightObj.SetActive(false);
			}
			else if (!_rightObj.gameObject.activeSelf)
			{
				_rightObj.gameObject.SetActive(true);
			}
		}
		
		private void UpdateObjPosStatus()
		{
			if (_dragging)
				return;
			if (_midObj.anchoredPosition.x <= _leftViewBound && _prevCount < _totalCount)
			{
				RectTransform temp = _leftObj;
				_leftObj = _midObj;
				_midObj = _rightObj;
				_rightObj = temp;
				_rightObj.anchoredPosition = new Vector2(_midObj.anchoredPosition.x + _preferredWidth, 0);
				_rightObj.SetSiblingIndex(2);
			}
			else if (_midObj.anchoredPosition.x >= _rightViewBound && _prevCount > 1)
			{
				RectTransform temp = _rightObj;
				_rightObj = _midObj;
				_midObj = _leftObj;
				_leftObj = temp;
				_leftObj.anchoredPosition = new Vector2(_midObj.anchoredPosition.x - _preferredWidth, 0);
				_leftObj.SetSiblingIndex(0);
			}
			SetOutBoundsItemActive();
		}

		private int GetFinalCharNumber(string objName)
		{
			return int.Parse(objName.Substring(objName.Length - 1));
		}
		
		private void UpdateItemAnchoredPosition(Vector2 position)
		{
			// 这里的position是根据计算得出的_midObj的posX，为了避免重复加载，具体情况具体处理
			if (currCount == _prevCount)
			{
				// currCount的值没有变过，说明_midObj仍然是_midObj，直接赋值
				_leftObj.anchoredPosition = new Vector2(position.x - _preferredWidth, 0);
				_midObj.anchoredPosition = new Vector2(position.x, 0);
				_rightObj.anchoredPosition = new Vector2(position.x + _preferredWidth, 0);
			}
			else if (currCount == _prevCount - 1)
			{
				// 当前_midObj是之前的_leftObj，将值进行处理，并对rightObj请求数据
				_leftObj.anchoredPosition = new Vector2(position.x, 0);
				_midObj.anchoredPosition = new Vector2(position.x + _preferredWidth, 0);
				_rightObj.anchoredPosition = new Vector2(position.x + _preferredWidth * 2, 0);
				// 向Lua请求数据，对象为_rightObj
				LuaFunctionHelper.Call(_requestItemContentUpdate, GetFinalCharNumber(_rightObj.name), currCount - 1);
			}
			else if (currCount == _prevCount + 1)
			{
				// 当前_midObj是之前的_rightObj
				_leftObj.anchoredPosition = new Vector2(position.x - _preferredWidth * 2, 0);
				_midObj.anchoredPosition = new Vector2(position.x - _preferredWidth, 0);
				_rightObj.anchoredPosition = new Vector2(position.x, 0);
				// 向Lua请求数据
				LuaFunctionHelper.Call(_requestItemContentUpdate, GetFinalCharNumber(_leftObj.name), currCount + 1);
			}
			else
			{
				// 全部重新赋值，并全部请求一次数据
				_leftObj.anchoredPosition = new Vector2(position.x - _preferredWidth, 0);
				_midObj.anchoredPosition = new Vector2(position.x, 0);
				_rightObj.anchoredPosition = new Vector2(position.x + _preferredWidth, 0);
				// 向Lua请求所有数据
				LuaFunctionHelper.Call(_requestAllContentUpdate, currCount);
			}
			UpdateLogicalBounds();
		}
		
		// 计算逻辑Content的左右边界是否进入了显示区域，如果进入了，则返回和显示区域边界的差值
		private Vector2 CalculateOffset(Vector2 delta)
		{
			Vector2 offset = Vector2.zero;
			_logicalLeftBound += delta.x;
			_logicalRightBound += delta.x;
			
			if (_logicalLeftBound > _leftViewBound)
			{
				offset.x = _leftViewBound - _logicalLeftBound;
			}

			if (_logicalRightBound < _rightViewBound)
			{
				offset.x = _rightViewBound - _logicalRightBound;
			}

			return offset;
		}

		private float RubberDelta(float overStretching, float viewSize)
		{
			return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
		}
		
		// Calculate logical bounds using total count and curr count
		private void UpdateLogicalBounds()
		{
			float currCenter = _midObj.anchoredPosition.x;
			_logicalLeftBound = currCenter - (currCount - 0.5f) * _preferredWidth;
			_logicalRightBound = currCenter + (_totalCount - currCount + 0.5f) * _preferredWidth;
		}

		private void SetLogicalNormalizedPosition(float value)
		{
			value = Mathf.Clamp01(value);
			UpdateLogicalBounds();
			float hiddenLength = (_totalCount - 1) * _preferredWidth;
			float leftHideLength = value * hiddenLength;
			float logicalLeftBound = -_preferredWidth / 2 - leftHideLength;
			if (Mathf.Abs(logicalLeftBound - _logicalLeftBound) < 0.01f)
				return;
			_logicalLeftBound = logicalLeftBound;
			_prevCount = currCount;
			currCount = Mathf.RoundToInt(leftHideLength / _preferredWidth) + 1;
			_velocity = Vector2.zero;
			float currCenter = logicalLeftBound + (currCount - 0.5f) * _preferredWidth;
			UpdateItemAnchoredPosition(new Vector2(currCenter, 0));
		}

		private float ConvertCountToLogicalNormalizedPosition(int count)
		{
			if (count < 1 || _totalCount == 1)
				return 0.0f;
			if (count > _totalCount)
				return 1.0f;
			return (count - 1.0f) / (_totalCount - 1);
		}

		private void JumpToIndex(int index)
		{
			if (index < 1 || index > _totalCount)
				return;
			_autoJumping = true;
			_autoTargetNormalizedPosition = ConvertCountToLogicalNormalizedPosition(index);
		}

		private void PrevItem()
		{
			JumpToIndex(currCount - 1);
		}

		private void NextItem()
		{
			JumpToIndex(currCount + 1);
		}

		public void Setup(int totalCount, int originCount)
		{
			_initialized = true;
			_totalCount = totalCount;
			_prevCount = originCount;
			currCount = originCount;
			LuaFunctionHelper.Call(_requestAllContentUpdate, currCount);
		}
		
		public void RegRequestItemContentUpdate(LuaFunction func)
		{
			_requestItemContentUpdate = func;
		}

		public void RegRequestAllContentUpdate(LuaFunction func)
		{
			_requestAllContentUpdate = func;
		}
	}
}
