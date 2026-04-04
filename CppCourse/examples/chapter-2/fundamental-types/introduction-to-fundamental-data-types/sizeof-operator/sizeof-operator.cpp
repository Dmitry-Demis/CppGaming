#include <iostream>

int main() {
    // sizeof возвращает размер в байтах (std::size_t), вычисляется на этапе компиляции
    std::cout << "sizeof(char)      = " << sizeof(char)      << " байт\n";
    std::cout << "sizeof(short)     = " << sizeof(short)     << " байт\n";
    std::cout << "sizeof(int)       = " << sizeof(int)       << " байт\n";
    std::cout << "sizeof(long)      = " << sizeof(long)      << " байт\n";
    std::cout << "sizeof(long long) = " << sizeof(long long) << " байт\n";
    std::cout << "sizeof(float)     = " << sizeof(float)     << " байт\n";
    std::cout << "sizeof(double)    = " << sizeof(double)    << " байт\n";
    std::cout << "sizeof(bool)      = " << sizeof(bool)      << " байт\n";
    std::cout << "sizeof(void*)     = " << sizeof(void*)     << " байт\n";

    // sizeof переменной == sizeof типа
    int x = 0;
    std::cout << "\nsizeof(x) == sizeof(int): "
              << (sizeof(x) == sizeof(int) ? "true" : "false") << '\n';

    // sizeof массива
    int arr[10];
    std::cout << "sizeof(arr[10])   = " << sizeof(arr) << " байт"
              << " (" << sizeof(arr)/sizeof(arr[0]) << " элементов)\n";

    return 0;
}
