#include <iostream>
#include <limits>
#include <climits>

int main() {
    // Размеры всех целочисленных типов на текущей платформе
    std::cout << "=== Размеры целочисленных типов ===\n";
    std::cout << "sizeof(short)     = " << sizeof(short)     << " байт\n";
    std::cout << "sizeof(int)       = " << sizeof(int)       << " байт\n";
    std::cout << "sizeof(long)      = " << sizeof(long)      << " байт\n";
    std::cout << "sizeof(long long) = " << sizeof(long long) << " байт\n";

    // Диапазоны через <climits>
    std::cout << "\n=== Диапазоны (signed) ===\n";
    std::cout << "short    : " << SHRT_MIN  << " … " << SHRT_MAX  << '\n';
    std::cout << "int      : " << INT_MIN   << " … " << INT_MAX   << '\n';
    std::cout << "long     : " << LONG_MIN  << " … " << LONG_MAX  << '\n';
    std::cout << "long long: " << LLONG_MIN << " … " << LLONG_MAX << '\n';

    // Суффиксы литералов
    short     s  = 32767;
    int       i  = 2147483647;
    long      l  = 2147483647L;
    long long ll = 9000000000LL;
    unsigned  u  = 42u;

    std::cout << "\n=== Суффиксы литералов ===\n";
    std::cout << "short s  = " << s  << '\n';
    std::cout << "int   i  = " << i  << '\n';
    std::cout << "long  l  = " << l  << "L\n";
    std::cout << "ll    ll = " << ll << "LL\n";
    std::cout << "uint  u  = " << u  << "u\n";

    return 0;
}
