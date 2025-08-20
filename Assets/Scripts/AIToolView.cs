using UnityEngine;

public class AIToolView : MonoBehaviour
{
    public GameObject extinguisherModel;
    public GameObject medicModel;

    public void ShowExtinguisher()
    {
        if (extinguisherModel) extinguisherModel.SetActive(true);
        if (medicModel) medicModel.SetActive(false);
    }

    public void ShowMedic()
    {
        if (extinguisherModel) extinguisherModel.SetActive(false);
        if (medicModel) medicModel.SetActive(true);
    }

    public void ShowNone()
    {
        if (extinguisherModel) extinguisherModel.SetActive(false);
        if (medicModel) medicModel.SetActive(false);
    }
}
