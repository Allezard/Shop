using Microsoft.EntityFrameworkCore;

namespace Shop.Workspace.Data;

/// <summary>
/// Каркас. Наполняется по заданиям модуля 02 и дальше.
///
/// Что здесь появится по ходу курса:
///   модуль 02 — сущности, ключи UUID v7, связи, комплексные типы, TPH
///   модуль 04 — границы агрегатов, маркер конкурентности
///   модуль 05 — индексы: частичные, покрывающие, триграммный
///   модуль 07 — таблица исходящих сообщений
///
/// Эталон: ../Shop.Reference (строки помечены номером модуля)
/// </summary>
public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Задание 2.2 начинается здесь.
    }
}