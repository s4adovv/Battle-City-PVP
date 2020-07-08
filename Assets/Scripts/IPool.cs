public interface IPool<T>
{

	T EnsureObject();

	void DestroyObject(T poolObject);

}
