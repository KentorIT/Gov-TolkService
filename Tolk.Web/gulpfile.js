/// <binding BeforeBuild='less' />
var gulp = require("gulp");
var less = require("gulp-less");

gulp.task("less", function () {
    return gulp.src("wwwroot/css/site.less")
        .pipe(less())
        .pipe(gulp.dest("wwwroot/css"));
});