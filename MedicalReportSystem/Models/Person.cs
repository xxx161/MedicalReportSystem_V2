using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace MedicalReportSystem.Models
{
    public class Person
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [Column("ID")]
        public long Id { get; set; }

        /// <summary>
        /// 上级ID（可空）
        /// </summary>
        [Column("上级ID")]
        public long? ParentId { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        [Required]
        [Column("编码", TypeName = "VARCHAR2")]
        [StringLength(10)]
        public string? Code { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        [Required]
        [Column("名称", TypeName = "VARCHAR2")]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// 简码（可空）
        /// </summary>
        [Column("简码", TypeName = "VARCHAR2")]
        [StringLength(100)]
        public string? ShortCode { get; set; }

        /// <summary>
        /// 位置（可空）
        /// </summary>
        [Column("位置", TypeName = "VARCHAR2")]
        [StringLength(50)]
        public string? Location { get; set; }

        /// <summary>
        /// 是否末级（默认0）
        /// </summary>
        [Column("末级")]
        public int? IsLeaf { get; set; } = 0;

        /// <summary>
        /// 建档时间（可空）
        /// </summary>
        [Column("建档时间", TypeName = "DATE")]
        public DateTime? CreateTime { get; set; }

        /// <summary>
        /// 撤档时间（可空）
        /// </summary>
        [Column("撤档时间", TypeName = "DATE")]
        public DateTime? DeleteTime { get; set; }

        /// <summary>
        /// 环境类别（可空）
        /// </summary>
        [Column("环境类别", TypeName = "VARCHAR2")]
        [StringLength(10)]
        public string? EnvType { get; set; }

        /// <summary>
        /// 部门负责人ID（可空）
        /// </summary>
        [Column("部门负责人")]
        public long? ManagerId { get; set; }

        /// <summary>
        /// 站点（可空）
        /// </summary>
        [Column("站点", TypeName = "VARCHAR2")]
        [StringLength(3)]
        public string? Site { get; set; }

        /// <summary>
        /// 顺序号（可空）
        /// </summary>
        [Column("顺序")]
        public int? SortOrder { get; set; }

        /// <summary>
        /// 最后修改时间（可空）
        /// </summary>
        [Column("最后修改时间", TypeName = "DATE")]
        public DateTime? LastModifiedTime { get; set; }

        /// <summary>
        /// 别名（可空）
        /// </summary>
        [Column("别名", TypeName = "VARCHAR2")]
        [StringLength(100)]
        public string? Alias { get; set; }

        /// <summary>
        /// 位置编码（可空）
        /// </summary>
        [Column("位置编码", TypeName = "VARCHAR2")]
        [StringLength(4)]
        public string? LocationCode { get; set; }

        /// <summary>
        /// 资源ID（默认使用Sys_Guid()生成）
        /// </summary>
        [Column("资源ID", TypeName = "VARCHAR2")]
        [StringLength(36)]
        public string ResourceId { get; set; } = Guid.NewGuid().ToString();
    }
}
