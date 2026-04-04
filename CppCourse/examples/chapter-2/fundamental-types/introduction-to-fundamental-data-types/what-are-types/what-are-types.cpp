#include <iostream>
#include <limits>

int main() {
    // Каждая переменная имеет тип — компилятор знает размер и операции
    int    count  = 42;
    double price  = 3.14;
    bool   active = true;
    char   letter = 'A';

    std::cout << "int    count  = " << count  << "  (sizeof=" << sizeof(count)  << ")\n";
    std::cout << "double price  = " << price  << "  (sizeof=" << sizeof(price)  << ")\n";
    std::cout << "bool   active = " << active << "  (sizeof=" << sizeof(active) << ")\n";
    std::cout << "char   letter = " << letter << "  (sizeof=" << sizeof(letter) << ")\n";

    // Тип определяет допустимые операции
    std::cout << "\ncount + 8 = " << count + 8 << '\n';
    std::cout << "price * 2 = " << price * 2 << '\n';
    std::cout << "!active   = " << !active   << '\n';

    return 0;
}
