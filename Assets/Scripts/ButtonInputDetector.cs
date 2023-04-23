using UnityEngine;

public class ButtonInputDetector : MonoBehaviour
{
    public bool IsRight { get; private set; }
    public bool IsLeft { get; private set; }
    public bool IsUp { get; private set; }
    public bool IsDown { get; private set; }
    public bool IsJump { get; private set; }

    // Update is called once per frame
    void Update()
    {
        IsRight = Input.GetKey(KeyCode.D);
        IsLeft = Input.GetKey(KeyCode.A);
        IsUp = Input.GetKey(KeyCode.W);
        IsDown = Input.GetKey(KeyCode.S);
        IsJump = Input.GetKey(KeyCode.Space);

    }
}
