using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheLongNight
{
	public class Bootstrap : MonoBehaviour
	{
		private void Awake()
		{
			SceneManager.LoadScene("MainMenu");
		}
	}
}
