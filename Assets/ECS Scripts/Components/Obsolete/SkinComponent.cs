using Morpeh;
using UnityEngine;
using Unity.IL2CPP.CompilerServices;

[Il2CppSetOption(Option.NullChecks, false)]
[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
[Il2CppSetOption(Option.DivideByZeroChecks, false)]
[System.Serializable]
public struct SkinComponent : IComponent {

	// From lower level to upper

	public Sprite[] bodySprites;
	public Sprite[] lightSprites;
	public Sprite[] shadowSprites;
	public Sprite[] wheelBodySprites;
	public Sprite[] wheelLightSprites;
	public Sprite[] wheelShadowSprites;

	// Every sprite will inherit colors

	public Color[] bodyColors;
	public Color[] lightColors;
	public Color[] shadowColors;

}