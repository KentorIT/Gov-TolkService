/* eslint-disable no-extra-parens, eqeqeq */

Date.prototype.equalsDate = function (otherDate) {
    return (
        this.getFullYear() === otherDate.getFullYear() &&
        this.getMonth() === otherDate.getMonth() &&
        this.getDate() === otherDate.getDate()
    );
};

Date.prototype.equalsDateTime = function (otherDate) {
    return (
        this.equalsDate(otherDate) &&
        this.getHours() === otherDate.getHours() &&
        this.getMinutes() === otherDate.getMinutes() &&
        this.getSeconds() === otherDate.getSeconds()
    );
};

// Auto-format date entered with just digits.
Date.customFormat = function (dateStr) {
    if (/^[0-9]{6}$/.test(dateStr)) {
        dateStr = "20" + dateStr;
    }
    if (/^[0-9]{8}$/.test(dateStr)) {
        dateStr = dateStr.substring(0, 4) + "-" + dateStr.substring(4, 6) + "-" + dateStr.substring(6);
    }
    return dateStr;
};

Date.prototype.addDays = function (amount) {
    this.setTime(this.getTime() + ((1000 * 60 * 60 * 24) * amount));
    return this;
};

// Alias for subtracting days, uses addDays method
Date.prototype.subtractDays = function (amount) {
    return this.addDays(-amount);
};

Date.prototype.zeroTime = function () {
    this.setHours(0, 0, 0, 0);
    return this;
};

Date.prototype.localDateTime = function () {
    this.setTime(this.getTime() - (this.getTimezoneOffset() * 60 * 1000)); // Compensate GMT timezone offset
    return this;
};