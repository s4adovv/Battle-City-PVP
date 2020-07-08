public interface IPool<T>
{

	T Get();

	void Remove(T poolObject);

}
