// Ввод и вывод: iostream
// std::cout — вывод на экран, std::cin — ввод с клавиатуры.

#include <iostream>

int main()
{
    // std::cout — вывод, оператор <<
    std::cout << "Введите ваш возраст: ";

    int age{};
    std::cin >> age;   // std::cin — ввод, оператор >>

    std::cout << "Вам " << age << " лет.\n";

    // \n vs std::endl:
    // \n — просто символ новой строки (быстрее)
    // std::endl — новая строка + сброс буфера (медленнее)
    std::cout << "Строка 1\n";
    std::cout << "Строка 2" << std::endl;  // сброс буфера

    // Цепочка вывода:
    int x{ 5 }, y{ 3 };
    std::cout << x << " + " << y << " = " << (x + y) << "\n";

    return 0;
}