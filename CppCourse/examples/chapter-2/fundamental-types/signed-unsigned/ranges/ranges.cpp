#include <iostream>
#include <limits>
#include <climits>

int main() {
    // Диапазоны знаковых типов
    std::cout << "=== Знаковые типы ===\n";
    std::cout << "signed char : " << (int)SCHAR_MIN << " … " << (int)SCHAR_MAX << '\n';
    std::cout << "short       : " << SHRT_MIN  << " … " << SHRT_MAX  << '\n';
    std::cout << "int         : " << INT_MIN   << " … " << INT_MAX   << '\n';
    std::cout << "long long   : " << LLONG_MIN << " … " << LLONG_MAX << '\n';

    // Диапазоны беззнаковых типов
    std::cout << "\n=== Беззнаковые типы ===\n";
    std::cout << "unsigned char  : 0 … " << (unsigned)UCHAR_MAX << '\n';
    std::cout << "unsigned short : 0 … " << USHRT_MAX << '\n';
    std::cout << "unsigned int   : 0 … " << UINT_MAX  << '\n';
    std::cout << "unsigned long long: 0 … " << ULLONG_MAX << '\n';

    // Через std::numeric_limits
    std::cout << "\n=== std::numeric_limits ===\n";
    std::cout << "int min = " << std::numeric_limits<int>::min() << '\n';
    std::cout << "int max = " << std::numeric_limits<int>::max() << '\n';
    std::cout << "uint max= " << std::numeric_limits<unsigned int>::max() << '\n';

    return 0;
}
