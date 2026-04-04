// Буферизация std::cin: два числа в одной строке
#include <iostream>

int main()
{
    std::cout << "Введите два числа через пробел: ";

    int x{};
    int y{};
    std::cin >> x >> y;  // оба значения из одного ввода

    std::cout << "Вы ввели: " << x << " и " << y << "\n";

    return 0;
}
