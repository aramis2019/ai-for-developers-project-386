/**
 * Границы полей для клиентской валидации.
 *
 * ИСТОЧНИК ИСТИНЫ — contracts/spec/models.tsp. Здесь они продублированы
 * только ради мгновенной обратной связи в формах: показать ошибку до отправки.
 *
 * Сервер остаётся последней инстанцией. Любое расхождение этих значений
 * с контрактом приведёт лишь к тому, что запрос уйдёт и вернётся `422
 * VALIDATION_FAILED` — оно будет показано пользователю. Клиент не может
 * «разрешить» то, что запретил контракт.
 *
 * При изменении ограничений в .tsp правьте и этот файл.
 */

export const constraints = {
  eventType: {
    /** `^[a-z0-9]+(?:-[a-z0-9]+)*$` — slug, участвует в публичных ссылках. */
    idPattern: /^[a-z0-9]+(?:-[a-z0-9]+)*$/,
    idMinLength: 1,
    idMaxLength: 64,
    titleMinLength: 1,
    titleMaxLength: 120,
    descriptionMaxLength: 1000,
    durationMin: 5,
    durationMax: 480,
  },
  guest: {
    nameMinLength: 1,
    nameMaxLength: 120,
    emailMaxLength: 254,
    noteMaxLength: 1000,
  },
} as const;

/** Простая проверка e-mail. Точную форму валидирует сервер. */
export const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
