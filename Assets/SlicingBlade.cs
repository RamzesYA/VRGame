using UnityEngine;
using EzySlice;

public class SlicingBlade : MonoBehaviour
{
    public Collider cuttingEdge;

    private const string SLICEABLE_TAG = "Cuttable";
    private const float FRAGMENT_LIFETIME = 10f; // Время жизни фрагментов в секундах

    private void OnImpactDetected(Collision impactData)
    {
        GameObject objectToSlice = impactData.gameObject;

        // Проверка возможности разрезания
        if (!objectToSlice.CompareTag(SLICEABLE_TAG)) return;

        // Защита от повторного разрезания
        if (objectToSlice.GetComponent<SliceMarker>() != null)
            return;

        // Добавляем метку разрезания
        objectToSlice.AddComponent<SliceMarker>();

        // Получаем точку контакта
        ContactPoint initialContact = impactData.contacts[0];
        Vector3 sliceOrigin = initialContact.point;

        // Вычисляем нормаль плоскости разреза
        Vector3 sliceDirection = Vector3.Cross(cuttingEdge.transform.forward, Vector3.up).normalized;

        // Получаем материал объекта
        Material sliceMaterial = GetTargetMaterial(objectToSlice);

        // Выполняем операцию разрезания
        PerformSlicingOperation(objectToSlice, sliceOrigin, sliceDirection, sliceMaterial);
    }

    private Material GetTargetMaterial(GameObject targetObject)
    {
        Renderer objectRenderer = targetObject.GetComponent<Renderer>();
        return objectRenderer?.material ?? new Material(Shader.Find("Standard"));
    }

    private void PerformSlicingOperation(GameObject originalObject, Vector3 cutPosition,
                                       Vector3 cutNormal, Material material)
    {
        SlicedHull slicingResult = originalObject.Slice(cutPosition, cutNormal, material);

        if (slicingResult != null)
        {
            // Создаем части объекта
            GameObject topFragment = slicingResult.CreateUpperHull(originalObject, material);
            GameObject bottomFragment = slicingResult.CreateLowerHull(originalObject, material);

            // Копируем трансформации
            TransferTransformData(originalObject.transform, topFragment.transform);
            TransferTransformData(originalObject.transform, bottomFragment.transform);

            // Настраиваем физику
            ConfigureFragmentPhysics(topFragment);
            ConfigureFragmentPhysics(bottomFragment);

            // Добавляем таймер удаления для фрагментов
            AddDestructionTimer(topFragment);
            AddDestructionTimer(bottomFragment);

            // Удаляем исходный объект
            Destroy(originalObject);
        }
    }

    private void ConfigureFragmentPhysics(GameObject fragment)
    {
        MeshCollider fragmentCollider = fragment.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        fragment.AddComponent<Rigidbody>();
    }

    private void TransferTransformData(Transform source, Transform destination)
    {
        destination.position = source.position;
        destination.rotation = source.rotation;
        destination.localScale = source.localScale;
    }

    private void AddDestructionTimer(GameObject fragment)
    {
        if (fragment != null)
        {
            // Добавляем компонент таймера удаления к фрагменту
            FragmentDestructionTimer destructionTimer = fragment.AddComponent<FragmentDestructionTimer>();
            destructionTimer.SetLifetime(FRAGMENT_LIFETIME);
        }
    }

    // Обработчик столкновений
    private void OnCollisionEnter(Collision collision) => OnImpactDetected(collision);
}

// Маркер для отслеживания разрезанных объектов
public class SliceMarker : MonoBehaviour { }

// Компонент для автоматического удаления фрагментов через заданное время
public class FragmentDestructionTimer : MonoBehaviour
{
    private float lifetime = 10f;
    private float creationTime;

    private void Start()
    {
        creationTime = Time.time;
    }

    private void Update()
    {
        // Проверяем, истекло ли время жизни
        if (Time.time - creationTime >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    // Метод для установки времени жизни (можно настроить для разных фрагментов)
    public void SetLifetime(float seconds)
    {
        lifetime = seconds;
    }

    // Опционально: можно добавить визуальную индикацию оставшегося времени
    private void OnDestroy()
    {
        // Здесь можно добавить эффекты при уничтожении (частицы, звук и т.д.)
        Debug.Log($"Fragment {gameObject.name} destroyed after {lifetime} seconds");
    }
}