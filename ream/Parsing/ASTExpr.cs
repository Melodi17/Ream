using Ream.Lexing;
using Ream.Interpreting;
using Ream.SDK;

namespace Ream.Parsing
{
   [Serializable] public abstract class Expr
   {
     public abstract T Accept<T>(Visitor<T> visitor);
     public interface Visitor<T>
     {
         public T VisitAssignExpr(Assign expr);
         public T VisitBinaryExpr(Binary expr);
         public T VisitTernaryExpr(Ternary expr);
         public T VisitCallExpr(Call expr);
         public T VisitIndexerExpr(Indexer expr);
         public T VisitMixerExpr(Mixer expr);
         public T VisitGetExpr(Get expr);
         public T VisitGroupingExpr(Grouping expr);
         public T VisitSequenceExpr(Sequence expr);
         public T VisitDictionaryExpr(Dictionary expr);
         public T VisitLambdaExpr(Lambda expr);
         public T VisitLiteralExpr(Literal expr);
         public T VisitLogicalExpr(Logical expr);
         public T VisitSetExpr(Set expr);
         public T VisitThisExpr(This expr);
         public T VisitUnaryExpr(Unary expr);
         public T VisitTranslateExpr(Translate expr);
         public T VisitVariableExpr(Variable expr);
     }
     [Serializable] public class Assign : Expr
      {
     public readonly Token name;
     public readonly Expr value;

         public Assign(Token name, Expr value)
          {
             this.name = name;
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitAssignExpr(this);
          }
      }

     [Serializable] public class Binary : Expr
      {
     public readonly Expr left;
     public readonly Token @operator;
     public readonly Expr right;

         public Binary(Expr left, Token @operator, Expr right)
          {
             this.left = left;
             this.@operator = @operator;
             this.right = right;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitBinaryExpr(this);
          }
      }

     [Serializable] public class Ternary : Expr
      {
     public readonly Expr left;
     public readonly Token leftOperator;
     public readonly Expr middle;
     public readonly Token rightOperator;
     public readonly Expr right;

         public Ternary(Expr left, Token leftOperator, Expr middle, Token rightOperator, Expr right)
          {
             this.left = left;
             this.leftOperator = leftOperator;
             this.middle = middle;
             this.rightOperator = rightOperator;
             this.right = right;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitTernaryExpr(this);
          }
      }

     [Serializable] public class Call : Expr
      {
     public readonly Expr callee;
     public readonly Token paren;
     public readonly List<Expr> arguments;

         public Call(Expr callee, Token paren, List<Expr> arguments)
          {
             this.callee = callee;
             this.paren = paren;
             this.arguments = arguments;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitCallExpr(this);
          }
      }

     [Serializable] public class Indexer : Expr
      {
     public readonly Expr callee;
     public readonly Token paren;
     public readonly Expr index;

         public Indexer(Expr callee, Token paren, Expr index)
          {
             this.callee = callee;
             this.paren = paren;
             this.index = index;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitIndexerExpr(this);
          }
      }

     [Serializable] public class Mixer : Expr
      {
     public readonly Expr callee;
     public readonly Token paren;
     public readonly Expr index;
     public readonly Expr value;

         public Mixer(Expr callee, Token paren, Expr index, Expr value)
          {
             this.callee = callee;
             this.paren = paren;
             this.index = index;
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitMixerExpr(this);
          }
      }

     [Serializable] public class Get : Expr
      {
     public readonly Expr obj;
     public readonly Token name;

         public Get(Expr obj, Token name)
          {
             this.obj = obj;
             this.name = name;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitGetExpr(this);
          }
      }

     [Serializable] public class Grouping : Expr
      {
     public readonly Expr expression;

         public Grouping(Expr expression)
          {
             this.expression = expression;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitGroupingExpr(this);
          }
      }

     [Serializable] public class Sequence : Expr
      {
     public readonly List<Expr> items;

         public Sequence(List<Expr> items)
          {
             this.items = items;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitSequenceExpr(this);
          }
      }

     [Serializable] public class Dictionary : Expr
      {
     public readonly Token paren;
     public readonly Dictionary<Expr,Expr> items;

         public Dictionary(Token paren, Dictionary<Expr,Expr> items)
          {
             this.paren = paren;
             this.items = items;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitDictionaryExpr(this);
          }
      }

     [Serializable] public class Lambda : Expr
      {
     public readonly List<Token> parameters;
     public readonly List<Stmt> body;

         public Lambda(List<Token> parameters, List<Stmt> body)
          {
             this.parameters = parameters;
             this.body = body;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitLambdaExpr(this);
          }
      }

     [Serializable] public class Literal : Expr
      {
     public readonly Object value;

         public Literal(Object value)
          {
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitLiteralExpr(this);
          }
      }

     [Serializable] public class Logical : Expr
      {
     public readonly Expr left;
     public readonly Token @operator;
     public readonly Expr right;

         public Logical(Expr left, Token @operator, Expr right)
          {
             this.left = left;
             this.@operator = @operator;
             this.right = right;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitLogicalExpr(this);
          }
      }

     [Serializable] public class Set : Expr
      {
     public readonly Expr obj;
     public readonly Token name;
     public readonly Expr value;

         public Set(Expr obj, Token name, Expr value)
          {
             this.obj = obj;
             this.name = name;
             this.value = value;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitSetExpr(this);
          }
      }

     [Serializable] public class This : Expr
      {
     public readonly Token keyword;

         public This(Token keyword)
          {
             this.keyword = keyword;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitThisExpr(this);
          }
      }

     [Serializable] public class Unary : Expr
      {
     public readonly Token @operator;
     public readonly Expr right;

         public Unary(Token @operator, Expr right)
          {
             this.@operator = @operator;
             this.right = right;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitUnaryExpr(this);
          }
      }

     [Serializable] public class Translate : Expr
      {
     public readonly Token @operator;
     public readonly Token name;

         public Translate(Token @operator, Token name)
          {
             this.@operator = @operator;
             this.name = name;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitTranslateExpr(this);
          }
      }

     [Serializable] public class Variable : Expr
      {
     public readonly Token name;

         public Variable(Token name)
          {
             this.name = name;
          }

          public override T Accept<T>(Visitor<T> visitor)
          {
             return visitor.VisitVariableExpr(this);
          }
      }

  }
}
