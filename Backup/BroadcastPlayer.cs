using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
	internal class Broadcast
	{
		public readonly GameObject go;
		public readonly Transform transform;
		public bool isOnShow;
		public readonly float length;

		public Broadcast(GameObject go, Transform transform, bool isOnShow, float length)
		{
			this.transform = transform;
			this.go = go;
			this.isOnShow = isOnShow;
			this.length = length;
		}
	}
	public class BroadcastPlayer : MonoBehaviour
	{
		// The space between every broadcast
		public float spacing = 50;
		public float speed = 0.5f;

		// Declare for instantiating new broadcasts
		[SerializeField]
		private GameObject singleBroadcastObject;
		[SerializeField]
		private Image icon;
		[SerializeField]
		private Text content;

		// Whether this broadcast is playing
		private bool _playing = true;

		// Store broadcasts
		private readonly List<Broadcast> _broadcasts = new List<Broadcast>();

		// Width of the whole broadcast view, used for calculating start position and end position
		private float _containerWidth;
		private float _leftBoundary;
		private float _rightBoundary;

		[SerializeField]
		private List<Sprite> sprites = new List<Sprite>();

		void Start()
		{
			_containerWidth = transform.GetComponent<RectTransform>().rect.width;
			_leftBoundary = -_containerWidth / 2;
			_rightBoundary = _containerWidth / 2;
			// Do not show broadcast view by default
			SetBroadCastStatus(false);
		}

		void Update()
		{
			var length = _broadcasts.Count;
			if (length <= 0)
				SetBroadCastStatus(false);

			if (!_playing) return;
			for (var i = length - 1; i > -1; i--)
			{
				var curr = _broadcasts[i];
				if (!curr.isOnShow)
				{
					if (i == 0 || PreviousBroadcastDisplayedCompletely(_broadcasts[i - 1]))
					{
						_broadcasts[i].isOnShow = true;
						_broadcasts[i].transform.Translate(Vector3.left * (Time.deltaTime * speed), Space.World);
					}

					continue;
				}

				if (curr.transform.localPosition.x + curr.length <= _leftBoundary)
				{
					Destroy(curr.go);
					_broadcasts.RemoveAt(i);
					continue;
				}

				curr.transform.Translate(Vector3.left * (Time.deltaTime * speed), Space.World);
			}
		}

		private bool PreviousBroadcastDisplayedCompletely(Broadcast prev)
		{
			return (prev.isOnShow && prev.transform.localPosition.x + prev.length + spacing <= _rightBoundary);
		}

		// Set content of this broadcast and play
		public void AddContent(string text, int iconIndex = 0)
		{
			// Initialize a new broadcast
			icon.sprite = sprites[iconIndex];
			content.text = text;
			var newBroadcast = Instantiate(singleBroadcastObject, transform);
			newBroadcast.transform.localPosition = new Vector3(_rightBoundary, 0, 0);
			var layoutSpacing = newBroadcast.GetComponent<HorizontalLayoutGroup>().spacing;
			var length = icon.rectTransform.rect.width + layoutSpacing + content.preferredWidth;
			newBroadcast.SetActive(true);
			
			// Declare a new broadcast
			var broadcast = new Broadcast(newBroadcast, newBroadcast.transform, false, length);
			_broadcasts.Add(broadcast);

			// Change background status
			SetBroadCastStatus(true);
		}

		public void ClearBroadCastList()
		{
			foreach(var broadcast in _broadcasts)
			{
				Destroy(broadcast.go);
			}
			_broadcasts.Clear();
			SetBroadCastStatus(false);
		}

		// Show or hide broadcast view according to _playing
		private void SetBroadCastStatus(bool playOrNot)
		{
			if (_playing == playOrNot) return;
			_playing = playOrNot;
			gameObject.SetActive(_playing);
		}
	}
}
